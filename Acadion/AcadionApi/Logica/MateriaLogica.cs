using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;

namespace AcadionApi.Logica
{
    public class MateriaLogica : IMateriaLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public MateriaLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<MateriaDto> RegistrarMateriaAsync(MateriaCrearDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Nombre))
            {
                throw new ArgumentException("El nombre de la materia es requerido.");
            }

            var nuevaMateria = new Materia
            {
                Nombre = dto.Nombre,
                Modalidad = dto.Modalidad,
                Estado = dto.Estado,
                IdAnio = dto.IdAnio
            };

            // Cargar correlativas si vienen IDs
            if (dto.CorrelativasIds != null && dto.CorrelativasIds.Any())
            {
                var materiasCompletas = await _unitOfWork.Materias.GetAllAsync();
                nuevaMateria.Correlativas = materiasCompletas
                    .Where(m => dto.CorrelativasIds.Contains(m.IdMateria)).ToList();
            }

            await _unitOfWork.Materias.AddAsync(nuevaMateria);
            await _unitOfWork.SaveChangesAsync();

            return new MateriaDto
            {
                IdMateria = nuevaMateria.IdMateria,
                Nombre = nuevaMateria.Nombre,
                Modalidad = nuevaMateria.Modalidad,
                Estado = nuevaMateria.Estado,
                IdAnio = nuevaMateria.IdAnio,
                Correlativas = nuevaMateria.Correlativas.Select(c => new MateriaResumenDto
                {
                    IdMateria = c.IdMateria,
                    Nombre = c.Nombre
                }).ToList()
            };
        }

        public async Task<IEnumerable<MateriaListaDto>> ObtenerMateriasAsync()
        {
            var materias = await _unitOfWork.Materias.GetAllAsync();
            return materias.Select(m => new MateriaListaDto
            {
                IdMateria = m.IdMateria,
                Nombre = m.Nombre,
                Modalidad = m.Modalidad,
                Estado = m.Estado,
                IdAnio = m.IdAnio
            });
        }

        public async Task<MateriaDto?> ObtenerMateriaPorIdAsync(int id)
        {
            var materia = await _unitOfWork.Materias.GetByIdAsync(id);
            if (materia == null) return null;

            // Nota: Dependiendo de tu repositorio genérico, puede que necesites incluir explícitamente la navegación de correlativas.
            // Si tu GetByIdAsync básico no trae relaciones, este mapeo devolverá vacío o puedes rellenarlo desde el UnitOfWork.
            return new MateriaDto
            {
                IdMateria = materia.IdMateria,
                Nombre = materia.Nombre,
                Modalidad = materia.Modalidad,
                Estado = materia.Estado,
                IdAnio = materia.IdAnio,
                Correlativas = materia.Correlativas?.Select(c => new MateriaResumenDto
                {
                    IdMateria = c.IdMateria,
                    Nombre = c.Nombre
                }).ToList() ?? new List<MateriaResumenDto>()
            };
        }

        public async Task<bool> ActualizarMateriaAsync(int id, MateriaActualizarDto dto)
        {
            if (dto == null) throw new ArgumentException("Datos inválidos.");

            var materia = await _unitOfWork.Materias.GetByIdAsync(id);
            if (materia == null) return false;

            materia.Nombre = dto.Nombre;
            materia.Modalidad = dto.Modalidad;
            materia.Estado = dto.Estado;
            materia.IdAnio = dto.IdAnio;

            // Actualizar Correlativas
            materia.Correlativas.Clear();
            if (dto.CorrelativasIds != null && dto.CorrelativasIds.Any())
            {
                var materiasCompletas = await _unitOfWork.Materias.GetAllAsync();
                materia.Correlativas = materiasCompletas
                    .Where(m => dto.CorrelativasIds.Contains(m.IdMateria)).ToList();
            }

            await _unitOfWork.Materias.UpdateAsync(materia);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarMateriaAsync(int id)
        {
            var materia = await _unitOfWork.Materias.GetByIdAsync(id);
            if (materia == null) return false;

            await _unitOfWork.Materias.DeleteAsync(materia);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}