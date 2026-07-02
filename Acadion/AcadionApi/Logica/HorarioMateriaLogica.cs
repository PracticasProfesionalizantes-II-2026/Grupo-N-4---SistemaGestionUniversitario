using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;

namespace AcadionApi.Logica
{
    public class HorarioMateriaLogica : IHorarioMateriaLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public HorarioMateriaLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<HorarioMateriaDto> RegistrarHorarioAsync(HorarioMateriaCrearDto dto)
        {
            if (dto == null || dto.IdMateria <= 0 || string.IsNullOrEmpty(dto.DiaSemana))
            {
                throw new ArgumentException("La materia y el día de la semana son requeridos.");
            }

            if (dto.HoraInicio >= dto.HoraFin)
            {
                throw new ArgumentException("La hora de inicio no puede ser mayor o igual a la hora de fin.");
            }

            var nuevoHorario = new HorarioMateria
            {
                IdMateria = dto.IdMateria,
                DiaSemana = dto.DiaSemana,
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

            var h = await _unitOfWork.HorariosMaterias.GetByIdAsync(id);
            if (h == null) return false;

            h.IdMateria = dto.IdMateria;
            h.DiaSemana = dto.DiaSemana;
            h.HoraInicio = dto.HoraInicio;
            h.HoraFin = dto.HoraFin;

            await _unitOfWork.HorariosMaterias.UpdateAsync(h);
            await _unitOfWork.SaveChangesAsync();
            return true;
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