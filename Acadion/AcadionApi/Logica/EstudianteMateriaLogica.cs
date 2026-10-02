using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;
using AcadionApi.Datos;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Logica
{
    public class EstudianteMateriaLogica : IEstudianteMateriaLogica
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public EstudianteMateriaLogica(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<InscripcionDto> InscribirEstudianteAsync(InscripcionCrearDto dto)
        {
            if (dto == null || dto.IdEstudiante <= 0 || dto.IdMateria <= 0)
            {
                throw new ArgumentException("Los identificadores de estudiante y materia son obligatorios.");
            }

            var estudiante = await _context.Usuarios.SingleOrDefaultAsync(u => u.Id == dto.IdEstudiante);
            if (estudiante is null || estudiante.RolId != RolesSistema.EstudianteId)
                throw new ArgumentException("El usuario indicado no es un estudiante.");

            var materia = await _context.Materias
                .Include(m => m.Correlativas)
                .Include(m => m.Horarios)
                .Include(m => m.AnioCursada)
                .SingleOrDefaultAsync(m => m.IdMateria == dto.IdMateria);
            if (materia is null || !string.Equals(materia.Estado, "Activa", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("La materia indicada no está disponible para inscripción.");
            if (!estudiante.CarreraId.HasValue || materia.AnioCursada?.IdCarrera != estudiante.CarreraId)
                throw new InvalidOperationException("La materia no pertenece a la carrera del estudiante.");
            if (estudiante.PlanEstudioId.HasValue && materia.PlanEstudioId != estudiante.PlanEstudioId)
                throw new InvalidOperationException("La materia no pertenece al plan de estudios del estudiante.");

            Comision? comision = null;
            if (dto.ComisionId.HasValue)
            {
                comision = await _context.Comisiones
                    .Include(c => c.Horarios)
                    .SingleOrDefaultAsync(c => c.Id == dto.ComisionId.Value);
                if (comision is null || !comision.Activa || comision.MateriaId != dto.IdMateria ||
                    comision.CicloLectivo != dto.CicloLectivo)
                    throw new ArgumentException("La comisión seleccionada no está disponible para esta materia y ciclo.");
                var ocupados = await _context.EstudianteMaterias.CountAsync(i =>
                    i.ComisionId == comision.Id && i.Estado != "Cancelada");
                if (!PoliticasAcademicas.HayVacante(comision.Cupo, ocupados))
                    throw new InvalidOperationException("La comisión alcanzó su cupo. Podés solicitar un lugar en la lista de espera.");
                dto.IdDocente = comision.DocenteId;
            }

            if (dto.IdDocente.HasValue)
            {
                var docente = await _context.Usuarios.SingleOrDefaultAsync(u => u.Id == dto.IdDocente.Value);
                if (docente is null || docente.RolId != RolesSistema.DocenteId)
                    throw new ArgumentException("El usuario indicado no es un docente.");

                if (!await _context.DocentesMaterias.AnyAsync(dm =>
                    dm.IdDocente == dto.IdDocente.Value && dm.IdMateria == dto.IdMateria &&
                    dm.CicloLectivo == dto.CicloLectivo && dm.Activa))
                    throw new ArgumentException("El docente no está asignado a esa materia y ciclo lectivo.");
            }

            if (await _context.EstudianteMaterias.AnyAsync(em =>
                em.IdEstudiante == dto.IdEstudiante && em.IdMateria == dto.IdMateria &&
                em.CicloLectivo == dto.CicloLectivo))
                throw new InvalidOperationException("El estudiante ya está inscripto en esa materia y ciclo lectivo.");

            var materiasRegularizadas = await _context.EstudianteMaterias
                .Where(em => em.IdEstudiante == dto.IdEstudiante &&
                    em.Estado != "Cancelada")
                .Select(em => new { em.IdMateria, em.Estado })
                .Distinct()
                .ToListAsync();
            var idsHabilitados = materiasRegularizadas
                .Where(em => PoliticasAcademicas.EstadoHabilitaCorrelativa(em.Estado))
                .Select(em => em.IdMateria);
            var idsPendientes = PoliticasAcademicas.CorrelativasPendientes(
                materia.Correlativas.Select(c => c.IdMateria), idsHabilitados);
            var correlativasPendientes = materia.Correlativas
                .Where(c => idsPendientes.Contains(c.IdMateria))
                .Select(c => c.Nombre)
                .OrderBy(nombre => nombre)
                .ToList();
            if (correlativasPendientes.Count > 0)
                throw new InvalidOperationException(
                    $"No podés inscribirte porque primero debés regularizar: {string.Join(", ", correlativasPendientes)}.");

            var horariosActuales = await _context.EstudianteMaterias
                .Where(em => em.IdEstudiante == dto.IdEstudiante &&
                    em.CicloLectivo == dto.CicloLectivo && em.Estado != "Cancelada")
                .Include(em => em.Materia).ThenInclude(m => m!.Horarios)
                .SelectMany(em => em.Materia!.Horarios)
                .ToListAsync();
            var horariosNuevos = comision is null
                ? materia.Horarios.Select(h => new { h.DiaSemana, h.HoraInicio, h.HoraFin }).ToList()
                : comision.Horarios.Select(h => new { h.DiaSemana, h.HoraInicio, h.HoraFin }).ToList();
            var existeSuperposicion = horariosNuevos.Any(nuevo => horariosActuales.Any(actual =>
                actual.DiaSemana == nuevo.DiaSemana &&
                PoliticasAcademicas.HorariosSeSuperponen(
                    nuevo.HoraInicio, nuevo.HoraFin, actual.HoraInicio, actual.HoraFin)));
            if (existeSuperposicion)
                throw new InvalidOperationException(
                    "No es posible inscribirse porque el horario se superpone con otra materia que ya cursás.");

            var nuevaInscripcion = new EstudianteMateria
            {
                IdEstudiante = dto.IdEstudiante,
                IdMateria = dto.IdMateria,
                ComisionId = dto.ComisionId,
                IdDocente = dto.IdDocente,
                CicloLectivo = dto.CicloLectivo,
                Cuatrimestre = materia.TipoCursada switch
                {
                    "Cuatrimestral" => $"{materia.NumeroPeriodo ?? 1}C",
                    "Bimestral" => $"{materia.NumeroPeriodo ?? 1}B",
                    _ => "Anual"
                },
                FechaInscripcion = DateTime.UtcNow,
                Estado = "Cursando"
            };

            await _unitOfWork.EstudiantesMaterias.AddAsync(nuevaInscripcion);
            await _unitOfWork.SaveChangesAsync();

            return new InscripcionDto
            {
                IdEstudianteMateria = nuevaInscripcion.IdEstudianteMateria,
                IdEstudiante = nuevaInscripcion.IdEstudiante,
                IdMateria = nuevaInscripcion.IdMateria,
                ComisionId = nuevaInscripcion.ComisionId,
                IdDocente = nuevaInscripcion.IdDocente,
                CicloLectivo = nuevaInscripcion.CicloLectivo,
                Cuatrimestre = nuevaInscripcion.Cuatrimestre,
                FechaInscripcion = nuevaInscripcion.FechaInscripcion,
                Estado = nuevaInscripcion.Estado
            };
        }

        public async Task<IEnumerable<InscripcionDto>> ObtenerInscripcionesAsync()
        {
            var lista = await _unitOfWork.EstudiantesMaterias.GetAllAsync();
            return lista.Select(i => new InscripcionDto
            {
                IdEstudianteMateria = i.IdEstudianteMateria,
                IdEstudiante = i.IdEstudiante,
                IdMateria = i.IdMateria,
                ComisionId = i.ComisionId,
                IdDocente = i.IdDocente,
                CicloLectivo = i.CicloLectivo,
                Cuatrimestre = i.Cuatrimestre,
                FechaInscripcion = i.FechaInscripcion,
                Estado = i.Estado
            });
        }

        public async Task<InscripcionDto?> ObtenerInscripcionPorIdAsync(int id)
        {
            var i = await _unitOfWork.EstudiantesMaterias.GetByIdAsync(id);
            if (i == null) return null;

            return new InscripcionDto
            {
                IdEstudianteMateria = i.IdEstudianteMateria,
                IdEstudiante = i.IdEstudiante,
                IdMateria = i.IdMateria,
                ComisionId = i.ComisionId,
                IdDocente = i.IdDocente,
                CicloLectivo = i.CicloLectivo,
                Cuatrimestre = i.Cuatrimestre,
                FechaInscripcion = i.FechaInscripcion,
                Estado = i.Estado
            };
        }

        public async Task<bool> ActualizarInscripcionAsync(int id, InscripcionActualizarDto dto)
        {
            if (dto == null) throw new ArgumentException("Datos inválidos.");

            var i = await _unitOfWork.EstudiantesMaterias.GetByIdAsync(id);
            if (i == null) return false;

            if (dto.IdDocente.HasValue && !await _context.DocentesMaterias.AnyAsync(dm =>
                dm.IdDocente == dto.IdDocente.Value && dm.IdMateria == i.IdMateria &&
                dm.CicloLectivo == dto.CicloLectivo && dm.Activa))
                throw new ArgumentException("El docente no está asignado a esa materia y ciclo lectivo.");

            i.IdDocente = dto.IdDocente;
            i.CicloLectivo = dto.CicloLectivo;
            i.Cuatrimestre = dto.Cuatrimestre;
            i.Estado = dto.Estado;

            await _unitOfWork.EstudiantesMaterias.UpdateAsync(i);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelarInscripcionAsync(int id)
        {
            var i = await _unitOfWork.EstudiantesMaterias.GetByIdAsync(id);
            if (i == null) return false;

            await _unitOfWork.EstudiantesMaterias.DeleteAsync(i);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
