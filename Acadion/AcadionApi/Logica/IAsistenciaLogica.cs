using System.Collections.Generic;
using System.Threading.Tasks;
using AcadionApi.DTOs;

namespace AcadionApi.Logica
{
    public interface IAsistenciaLogica
    {
        Task<AsistenciaDto> RegistrarAsistenciaAsync(AsistenciaCrearDto dto);
        Task<IEnumerable<AsistenciaDto>> ObtenerAsistenciasAsync();
        Task<AsistenciaDto?> ObtenerAsistenciaPorIdAsync(int id);
        Task<bool> ActualizarAsistenciaAsync(int id, AsistenciaActualizarDto dto);
        Task<bool> EliminarAsistenciaAsync(int id);
    }
}