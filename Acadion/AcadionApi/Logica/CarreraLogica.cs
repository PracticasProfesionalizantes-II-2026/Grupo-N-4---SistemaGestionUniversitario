using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;

namespace AcadionApi.Logica
{
    public class CarreraLogica : ICarreraLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public CarreraLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CarreraDto> RegistrarCarreraAsync(CarreraCrearDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Nombre))
            {
                throw new ArgumentException("El nombre de la carrera es requerido.");
            }

            var nuevaCarrera = new Carrera
            {
                Nombre = dto.Nombre,
                PlanEstudios = dto.PlanEstudios
            };

            await _unitOfWork.Carreras.AddAsync(nuevaCarrera);
            await _unitOfWork.SaveChangesAsync();

            return new CarreraDto
            {
                IdCarrera = nuevaCarrera.IdCarrera,
                Nombre = nuevaCarrera.Nombre,
                PlanEstudios = nuevaCarrera.PlanEstudios
            };
        }

        public async Task<IEnumerable<CarreraDto>> ObtenerCarrerasAsync()
        {
            var lista = await _unitOfWork.Carreras.GetAllAsync();
            return lista.Select(c => new CarreraDto
            {
                IdCarrera = c.IdCarrera,
                Nombre = c.Nombre,
                PlanEstudios = c.PlanEstudios
            });
        }

        public async Task<CarreraDto?> ObtenerCarreraPorIdAsync(int id)
        {
            var c = await _unitOfWork.Carreras.GetByIdAsync(id);
            if (c == null) return null;

            return new CarreraDto
            {
                IdCarrera = c.IdCarrera,
                Nombre = c.Nombre,
                PlanEstudios = c.PlanEstudios
            };
        }

        public async Task<bool> ActualizarCarreraAsync(int id, CarreraActualizarDto dto)
        {
            if (dto == null) throw new ArgumentException("Datos inválidos.");

            var c = await _unitOfWork.Carreras.GetByIdAsync(id);
            if (c == null) return false;

            c.Nombre = dto.Nombre;
            c.PlanEstudios = dto.PlanEstudios;

            await _unitOfWork.Carreras.UpdateAsync(c);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarCarreraAsync(int id)
        {
            var c = await _unitOfWork.Carreras.GetByIdAsync(id);
            if (c == null) return false;

            await _unitOfWork.Carreras.DeleteAsync(c);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}