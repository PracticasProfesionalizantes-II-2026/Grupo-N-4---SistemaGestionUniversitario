using System.Collections.Generic;
using System.Threading.Tasks;
using AcadionApi.DTOs;

namespace AcadionApi.Logica
{
    public interface IMateriaLogica
    {
        Task<MateriaDto> RegistrarMateriaAsync(MateriaCrearDto dto);
        Task<IEnumerable<MateriaListaDto>> ObtenerMateriasAsync();
        Task<MateriaDto?> ObtenerMateriaPorIdAsync(int id);
        Task<bool> ActualizarMateriaAsync(int id, MateriaActualizarDto dto);
        Task<bool> EliminarMateriaAsync(int id);
    }
}