using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;

namespace AcadionApi.Logica
{
    public class AsistenciaLogica : IAsistenciaLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public AsistenciaLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AsistenciaDto> RegistrarAsistenciaAsync(AsistenciaCrearDto dto)
        {
            if (dto == null || dto.IdEstudianteMateria <= 0 || string.IsNullOrEmpty(dto.Tipo))
            {
                throw new ArgumentException("La inscripción del estudiante y el tipo de asistencia son requeridos.");
            }

            var nuevaAsistencia = new Asistencia
            {
                IdEstudianteMateria = dto.IdEstudianteMateria,
                IdDocente = dto.IdDocente,
                Fecha = dto.Fecha,
                Tipo = dto.Tipo,
                Observaciones = dto.Observaciones
            };

            await _unitOfWork.Asistencias.AddAsync(nuevaAsistencia);
            await _unitOfWork.SaveChangesAsync();

            return new AsistenciaDto
            {
                IdAsistencia = nuevaAsistencia.IdAsistencia,
                IdEstudianteMateria = nuevaAsistencia.IdEstudianteMateria,
                IdDocente = nuevaAsistencia.IdDocente,
                Fecha = nuevaAsistencia.Fecha,
                Tipo = nuevaAsistencia.Tipo,
                Observaciones = nuevaAsistencia.Observaciones
            };
        }

        public async Task<IEnumerable<AsistenciaDto>> ObtenerAsistenciasAsync()
        {
            var lista = await _unitOfWork.Asistencias.GetAllAsync();
            return lista.Select(a => new AsistenciaDto
            {
                IdAsistencia = a.IdAsistencia,
                IdEstudianteMateria = a.IdEstudianteMateria,
                IdDocente = a.IdDocente,
                Fecha = a.Fecha,
                Tipo = a.Tipo,
                Observaciones = a.Observaciones
            });
        }

        public async Task<AsistenciaDto?> ObtenerAsistenciaPorIdAsync(int id)
        {
            var a = await _unitOfWork.Asistencias.GetByIdAsync(id);
            if (a == null) return null;

            return new AsistenciaDto
            {
                IdAsistencia = a.IdAsistencia,
                IdEstudianteMateria = a.IdEstudianteMateria,
                IdDocente = a.IdDocente,
                Fecha = a.Fecha,
                Tipo = a.Tipo,
                Observaciones = a.Observaciones
            };
        }

        public async Task<bool> ActualizarAsistenciaAsync(int id, AsistenciaActualizarDto dto)
        {
            if (dto == null) throw new ArgumentException("Datos inválidos.");

            var a = await _unitOfWork.Asistencias.GetByIdAsync(id);
            if (a == null) return false;

            a.IdDocente = dto.IdDocente;
            a.Fecha = dto.Fecha;
            a.Tipo = dto.Tipo;
            a.Observaciones = dto.Observaciones;

            await _unitOfWork.Asistencias.UpdateAsync(a);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarAsistenciaAsync(int id)
        {
            var a = await _unitOfWork.Asistencias.GetByIdAsync(id);
            if (a == null) return false;

            await _unitOfWork.Asistencias.DeleteAsync(a);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}