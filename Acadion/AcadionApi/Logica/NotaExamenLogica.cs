using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;

namespace AcadionApi.Logica
{
    public class NotaExamenLogica : INotaExamenLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotaExamenLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            var nuevaNota = new NotaExamen
            {
                IdExamen = dto.IdExamen,
                IdEstudiante = dto.IdEstudiante,
                Nota = dto.Nota,
                Observaciones = dto.Observaciones
            };

            await _unitOfWork.NotasExamenes.AddAsync(nuevaNota);
            await _unitOfWork.SaveChangesAsync();

            return new NotaExamenDto
            {
                IdNota = nuevaNota.IdNota,
                IdExamen = nuevaNota.IdExamen,
                IdEstudiante = nuevaNota.IdEstudiante,
                Nota = nuevaNota.Nota,
                Observaciones = nuevaNota.Observaciones
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
                Observaciones = n.Observaciones
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
                Observaciones = n.Observaciones
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

            await _unitOfWork.NotasExamenes.UpdateAsync(n);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

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