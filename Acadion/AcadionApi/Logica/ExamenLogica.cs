using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;

namespace AcadionApi.Logica
{
    public class ExamenLogica : IExamenLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public ExamenLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ExamenDto> RegistrarExamenAsync(ExamenCrearDto dto)
        {
            if (dto == null || dto.IdMateria <= 0 || string.IsNullOrEmpty(dto.TipoExamen))
            {
                throw new ArgumentException("La materia y el tipo de examen son requeridos.");
            }

            var nuevoExamen = new Examen
            {
                IdMateria = dto.IdMateria,
                CicloLectivo = dto.CicloLectivo,
                IdDocente = dto.IdDocente,
                Fecha = dto.Fecha,
                TipoExamen = dto.TipoExamen
            };

            await _unitOfWork.Examenes.AddAsync(nuevoExamen);
            await _unitOfWork.SaveChangesAsync();

            return new ExamenDto
            {
                IdExamen = nuevoExamen.IdExamen,
                IdMateria = nuevoExamen.IdMateria,
                CicloLectivo = nuevoExamen.CicloLectivo,
                IdDocente = nuevoExamen.IdDocente,
                Fecha = nuevoExamen.Fecha,
                TipoExamen = nuevoExamen.TipoExamen
            };
        }

        public async Task<IEnumerable<ExamenDto>> ObtenerExamenesAsync()
        {
            var lista = await _unitOfWork.Examenes.GetAllAsync();
            return lista.Select(e => new ExamenDto
            {
                IdExamen = e.IdExamen,
                IdMateria = e.IdMateria,
                CicloLectivo = e.CicloLectivo,
                IdDocente = e.IdDocente,
                Fecha = e.Fecha,
                TipoExamen = e.TipoExamen
            });
        }

        public async Task<ExamenDto?> ObtenerExamenPorIdAsync(int id)
        {
            var e = await _unitOfWork.Examenes.GetByIdAsync(id);
            if (e == null) return null;

            return new ExamenDto
            {
                IdExamen = e.IdExamen,
                IdMateria = e.IdMateria,
                CicloLectivo = e.CicloLectivo,
                IdDocente = e.IdDocente,
                Fecha = e.Fecha,
                TipoExamen = e.TipoExamen
            };
        }

        public async Task<bool> ActualizarExamenAsync(int id, ExamenActualizarDto dto)
        {
            if (dto == null) throw new ArgumentException("Datos inválidos.");

            var e = await _unitOfWork.Examenes.GetByIdAsync(id);
            if (e == null) return false;

            e.IdMateria = dto.IdMateria;
            e.CicloLectivo = dto.CicloLectivo;
            e.IdDocente = dto.IdDocente;
            e.Fecha = dto.Fecha;
            e.TipoExamen = dto.TipoExamen;

            await _unitOfWork.Examenes.UpdateAsync(e);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarExamenAsync(int id)
        {
            var e = await _unitOfWork.Examenes.GetByIdAsync(id);
            if (e == null) return false;

            await _unitOfWork.Examenes.DeleteAsync(e);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}