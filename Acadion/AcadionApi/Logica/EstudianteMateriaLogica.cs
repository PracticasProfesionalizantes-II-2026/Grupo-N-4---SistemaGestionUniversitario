using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;

namespace AcadionApi.Logica
{
    public class EstudianteMateriaLogica : IEstudianteMateriaLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public EstudianteMateriaLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<InscripcionDto> InscribirEstudianteAsync(InscripcionCrearDto dto)
        {
            if (dto == null || dto.IdEstudiante <= 0 || dto.IdMateria <= 0)
            {
                throw new ArgumentException("Los identificadores de estudiante y materia son obligatorios.");
            }

            // Aquí podrías ejecutar tu método entidad: nuevaInscripcion.ValidarCorrelativas(estudiante);

            var nuevaInscripcion = new EstudianteMateria
            {
                IdEstudiante = dto.IdEstudiante,
                IdMateria = dto.IdMateria,
                IdDocente = dto.IdDocente,
                CicloLectivo = dto.CicloLectivo,
                Cuatrimestre = dto.Cuatrimestre,
                FechaInscripcion = DateTime.Now,
                Estado = "Cursando"
            };

            await _unitOfWork.EstudiantesMaterias.AddAsync(nuevaInscripcion);
            await _unitOfWork.SaveChangesAsync();

            return new InscripcionDto
            {
                IdEstudianteMateria = nuevaInscripcion.IdEstudianteMateria,
                IdEstudiante = nuevaInscripcion.IdEstudiante,
                IdMateria = nuevaInscripcion.IdMateria,
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