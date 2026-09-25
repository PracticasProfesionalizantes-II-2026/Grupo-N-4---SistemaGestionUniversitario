using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;
using AcadionApi.Datos;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Logica
{
    public class NotaExamenLogica : INotaExamenLogica
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public NotaExamenLogica(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<NotaExamenDto> RegistrarNotaAsync(NotaExamenCrearDto dto)
        {
            if (dto == null || dto.IdExamen <= 0 || dto.IdEstudiante <= 0)
            {
                throw new ArgumentException("El examen, el estudiante y la nota son requeridos.");
            }

            if (dto.Nota < 0 || dto.Nota > 10)
            {
                throw new ArgumentException("La calificación debe estar comprendida entre 0 y 10.");
            }

            var examen = await _context.Set<Examen>().SingleOrDefaultAsync(e => e.IdExamen == dto.IdExamen)
                ?? throw new ArgumentException("El examen indicado no existe.");
            if (!await _context.EstudianteMaterias.AnyAsync(em =>
                em.IdEstudiante == dto.IdEstudiante && em.IdMateria == examen.IdMateria &&
                em.CicloLectivo == examen.CicloLectivo && em.Estado != "Cancelada"))
                throw new ArgumentException("El estudiante no está inscripto en la materia del examen.");
            if (examen.TipoExamen.Equals("Final", StringComparison.OrdinalIgnoreCase) &&
                !await _context.InscripcionesExamenes.AnyAsync(i =>
                    i.ExamenId == examen.IdExamen && i.EstudianteId == dto.IdEstudiante))
                throw new ArgumentException("El estudiante no está inscripto en este examen final.");

            if (await _context.Set<NotaExamen>().AnyAsync(n =>
                n.IdExamen == dto.IdExamen && n.IdEstudiante == dto.IdEstudiante))
                throw new InvalidOperationException("El estudiante ya tiene una nota cargada para este examen.");

            var nuevaNota = new NotaExamen
            {
                IdExamen = dto.IdExamen,
                IdEstudiante = dto.IdEstudiante,
                Nota = dto.Nota,
                Observaciones = dto.Observaciones,
                Condicion = CalcularCondicion(examen, dto.Nota)
            };

            await _unitOfWork.NotasExamenes.AddAsync(nuevaNota);
            await _unitOfWork.SaveChangesAsync();

            return new NotaExamenDto
            {
                IdNota = nuevaNota.IdNota,
                IdExamen = nuevaNota.IdExamen,
                IdEstudiante = nuevaNota.IdEstudiante,
                Nota = nuevaNota.Nota,
                Observaciones = nuevaNota.Observaciones,
                Condicion = nuevaNota.Condicion
            };
        }

        public async Task<IEnumerable<NotaExamenDto>> ObtenerNotasAsync()
        {
            var lista = await _unitOfWork.NotasExamenes.GetAllAsync();
            return lista.Select(n => new NotaExamenDto
            {
                IdNota = n.IdNota,
                IdExamen = n.IdExamen,
                IdEstudiante = n.IdEstudiante,
                Nota = n.Nota,
                Observaciones = n.Observaciones,
                Condicion = n.Condicion
            });
        }

        public async Task<NotaExamenDto?> ObtenerNotaPorIdAsync(int id)
        {
            var n = await _unitOfWork.NotasExamenes.GetByIdAsync(id);
            if (n == null) return null;

            return new NotaExamenDto
            {
                IdNota = n.IdNota,
                IdExamen = n.IdExamen,
                IdEstudiante = n.IdEstudiante,
                Nota = n.Nota,
                Observaciones = n.Observaciones,
                Condicion = n.Condicion
            };
        }

        public async Task<bool> ActualizarNotaAsync(int id, NotaExamenActualizarDto dto)
        {
            if (dto == null) throw new ArgumentException("Datos inválidos.");
            
            if (dto.Nota < 0 || dto.Nota > 10)
            {
                throw new ArgumentException("La calificación debe estar comprendida entre 0 y 10.");
            }

            var n = await _unitOfWork.NotasExamenes.GetByIdAsync(id);
            if (n == null) return false;

            n.IdExamen = dto.IdExamen;
            n.IdEstudiante = dto.IdEstudiante;
            n.Nota = dto.Nota;
            n.Observaciones = dto.Observaciones;
            var examen = await _context.Set<Examen>().SingleAsync(e => e.IdExamen == dto.IdExamen);
            n.Condicion = CalcularCondicion(examen, dto.Nota);

            await _unitOfWork.NotasExamenes.UpdateAsync(n);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static string CalcularCondicion(Examen examen, decimal nota) =>
            examen.TipoExamen.Equals("Final", StringComparison.OrdinalIgnoreCase) && nota >= examen.NotaMinimaRegularizacion
                ? "Aprobó"
                : examen.NotaMinimaPromocion.HasValue && nota >= examen.NotaMinimaPromocion.Value
                ? "Promocionó"
                : nota >= examen.NotaMinimaRegularizacion
                    ? "Regularizó"
                    : "Desaprobó";

        public async Task<bool> EliminarNotaAsync(int id)
        {
            var n = await _unitOfWork.NotasExamenes.GetByIdAsync(id);
            if (n == null) return false;

            await _unitOfWork.NotasExamenes.DeleteAsync(n);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
