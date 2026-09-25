using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Datos;
using AcadionApi.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Logica
{
    public class HorarioMateriaLogica : IHorarioMateriaLogica
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public HorarioMateriaLogica(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<HorarioMateriaDto> RegistrarHorarioAsync(HorarioMateriaCrearDto dto)
        {
            if (dto == null || dto.IdMateria <= 0 || string.IsNullOrWhiteSpace(dto.DiaSemana))
            {
                throw new ArgumentException("La materia y el día de la semana son requeridos.");
            }

            if (dto.HoraInicio >= dto.HoraFin)
            {
                throw new ArgumentException("La hora de inicio no puede ser mayor o igual a la hora de fin.");
            }
            if (dto.HoraFin - dto.HoraInicio < TimeSpan.FromMinutes(40))
                throw new ArgumentException("Cada horario de cursado debe durar como mínimo 40 minutos.");

            var diaSemana = await ValidarDisponibilidadAsync(dto.IdMateria, dto.DiaSemana,
                dto.HoraInicio, dto.HoraFin);

            var nuevoHorario = new HorarioMateria
            {
                IdMateria = dto.IdMateria,
                DiaSemana = diaSemana,
                HoraInicio = dto.HoraInicio,
                HoraFin = dto.HoraFin
            };

            await _unitOfWork.HorariosMaterias.AddAsync(nuevoHorario);
            await _unitOfWork.SaveChangesAsync();

            return new HorarioMateriaDto
            {
                IdHorarioMateria = nuevoHorario.IdHorarioMateria,
                IdMateria = nuevoHorario.IdMateria,
                DiaSemana = nuevoHorario.DiaSemana,
                HoraInicio = nuevoHorario.HoraInicio,
                HoraFin = nuevoHorario.HoraFin
            };
        }

        public async Task<IEnumerable<HorarioMateriaDto>> ObtenerHorariosAsync()
        {
            var lista = await _unitOfWork.HorariosMaterias.GetAllAsync();
            return lista.Select(h => new HorarioMateriaDto
            {
                IdHorarioMateria = h.IdHorarioMateria,
                IdMateria = h.IdMateria,
                DiaSemana = h.DiaSemana,
                HoraInicio = h.HoraInicio,
                HoraFin = h.HoraFin
            });
        }

        public async Task<HorarioMateriaDto?> ObtenerHorarioPorIdAsync(int id)
        {
            var h = await _unitOfWork.HorariosMaterias.GetByIdAsync(id);
            if (h == null) return null;

            return new HorarioMateriaDto
            {
                IdHorarioMateria = h.IdHorarioMateria,
                IdMateria = h.IdMateria,
                DiaSemana = h.DiaSemana,
                HoraInicio = h.HoraInicio,
                HoraFin = h.HoraFin
            };
        }

        public async Task<bool> ActualizarHorarioAsync(int id, HorarioMateriaActualizarDto dto)
        {
            if (dto == null) throw new ArgumentException("Datos inválidos.");
            if (dto.HoraInicio >= dto.HoraFin) throw new ArgumentException("Rango horario inválido.");
            if (dto.HoraFin - dto.HoraInicio < TimeSpan.FromMinutes(40))
                throw new ArgumentException("Cada horario de cursado debe durar como mínimo 40 minutos.");

            var h = await _unitOfWork.HorariosMaterias.GetByIdAsync(id);
            if (h == null) return false;

            var diaSemana = await ValidarDisponibilidadAsync(dto.IdMateria, dto.DiaSemana,
                dto.HoraInicio, dto.HoraFin, id);

            h.IdMateria = dto.IdMateria;
            h.DiaSemana = diaSemana;
            h.HoraInicio = dto.HoraInicio;
            h.HoraFin = dto.HoraFin;

            await _unitOfWork.HorariosMaterias.UpdateAsync(h);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private async Task<string> ValidarDisponibilidadAsync(int materiaId, string dia,
            TimeSpan horaInicio, TimeSpan horaFin, int? horarioExcluidoId = null)
        {
            var diasValidos = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
            var diaNormalizado = diasValidos.FirstOrDefault(d =>
                d.Equals(dia.Trim(), StringComparison.OrdinalIgnoreCase));
            if (diaNormalizado is null)
                throw new ArgumentException("El día de cursado no es válido.");

            var materia = await _context.Materias.AsNoTracking()
                .SingleOrDefaultAsync(m => m.IdMateria == materiaId)
                ?? throw new ArgumentException("La materia indicada no existe.");

            var conflicto = await _context.Set<HorarioMateria>().AsNoTracking()
                .Where(h => h.IdHorarioMateria != horarioExcluidoId &&
                    h.DiaSemana == diaNormalizado &&
                    h.HoraInicio < horaFin && horaInicio < h.HoraFin)
                .Join(_context.Materias.AsNoTracking(), h => h.IdMateria, m => m.IdMateria,
                    (h, m) => new { Horario = h, Materia = m })
                .Where(x => x.Materia.IdAnio == materia.IdAnio)
                .Select(x => new
                {
                    x.Materia.Nombre,
                    x.Horario.HoraInicio,
                    x.Horario.HoraFin
                })
                .FirstOrDefaultAsync();

            if (conflicto is not null)
                throw new InvalidOperationException(
                    $"El horario se superpone con {conflicto.Nombre} ({diaNormalizado} " +
                    $"{conflicto.HoraInicio:hh\\:mm}–{conflicto.HoraFin:hh\\:mm}) del mismo año y carrera.");

            return diaNormalizado;
        }

        public async Task<bool> EliminarHorarioAsync(int id)
        {
            var h = await _unitOfWork.HorariosMaterias.GetByIdAsync(id);
            if (h == null) return false;

            await _unitOfWork.HorariosMaterias.DeleteAsync(h);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
