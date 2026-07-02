using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;

namespace AcadionApi.Logica
{
    public class AnioLogica : IAnioLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public AnioLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AnioDto> RegistrarAnioAsync(AnioCrearDto dto)
        {
            if (dto == null || dto.NumeroAnio <= 0 || dto.IdCarrera <= 0)
            {
                throw new ArgumentException("El número de año y el ID de carrera son requeridos.");
            }

            var nuevoAnio = new Anio
            {
                NumeroAnio = dto.NumeroAnio,
                NombreAnio = dto.NombreAnio,
                IdCarrera = dto.IdCarrera
            };

            await _unitOfWork.Anios.AddAsync(nuevoAnio);
            await _unitOfWork.SaveChangesAsync();

            return new AnioDto
            {
                IdAnio = nuevoAnio.IdAnio,
                NumeroAnio = nuevoAnio.NumeroAnio,
                NombreAnio = nuevoAnio.NombreAnio,
                IdCarrera = nuevoAnio.IdCarrera
            };
        }

        public async Task<IEnumerable<AnioDto>> ObtenerAniosAsync()
        {
            var lista = await _unitOfWork.Anios.GetAllAsync();
            return lista.Select(a => new AnioDto
            {
                IdAnio = a.IdAnio,
                NumeroAnio = a.NumeroAnio,
                NombreAnio = a.NombreAnio,
                IdCarrera = a.IdCarrera
            });
        }

        public async Task<AnioDto?> ObtenerAnioPorIdAsync(int id)
        {
            var a = await _unitOfWork.Anios.GetByIdAsync(id);
            if (a == null) return null;

            return new AnioDto
            {
                IdAnio = a.IdAnio,
                NumeroAnio = a.NumeroAnio,
                NombreAnio = a.NombreAnio,
                IdCarrera = a.IdCarrera
            };
        }

        public async Task<bool> ActualizarAnioAsync(int id, AnioActualizarDto dto)
        {
            if (dto == null) throw new ArgumentException("Datos inválidos.");

            var a = await _unitOfWork.Anios.GetByIdAsync(id);
            if (a == null) return false;

            a.NumeroAnio = dto.NumeroAnio;
            a.NombreAnio = dto.NombreAnio;
            a.IdCarrera = dto.IdCarrera;

            await _unitOfWork.Anios.UpdateAsync(a);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarAnioAsync(int id)
        {
            var a = await _unitOfWork.Anios.GetByIdAsync(id);
            if (a == null) return false;

            await _unitOfWork.Anios.DeleteAsync(a);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}