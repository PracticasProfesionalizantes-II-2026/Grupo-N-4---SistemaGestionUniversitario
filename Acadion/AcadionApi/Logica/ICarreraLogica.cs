using System.Collections.Generic;
using System.Threading.Tasks;
using AcadionApi.DTOs;

namespace AcadionApi.Logica
{
    public interface ICarreraLogica
    {
        Task<CarreraDto> RegistrarCarreraAsync(CarreraCrearDto dto);
        Task<IEnumerable<CarreraDto>> ObtenerCarrerasAsync();
        Task<CarreraDto?> ObtenerCarreraPorIdAsync(int id);
        Task<bool> ActualizarCarreraAsync(int id, CarreraActualizarDto dto);
        Task<bool> EliminarCarreraAsync(int id);
    }
}