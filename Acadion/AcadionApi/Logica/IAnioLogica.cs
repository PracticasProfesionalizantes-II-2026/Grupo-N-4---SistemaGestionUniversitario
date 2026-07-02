using System.Collections.Generic;
using System.Threading.Tasks;
using AcadionApi.DTOs;

namespace AcadionApi.Logica
{
    public interface IAnioLogica
    {
        Task<AnioDto> RegistrarAnioAsync(AnioCrearDto dto);
        Task<IEnumerable<AnioDto>> ObtenerAniosAsync();
        Task<AnioDto?> ObtenerAnioPorIdAsync(int id);
        Task<bool> ActualizarAnioAsync(int id, AnioActualizarDto dto);
        Task<bool> EliminarAnioAsync(int id);
    }
}