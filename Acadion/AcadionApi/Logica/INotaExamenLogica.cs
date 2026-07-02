using System.Collections.Generic;
using System.Threading.Tasks;
using AcadionApi.DTOs;

namespace AcadionApi.Logica
{
    public interface INotaExamenLogica
    {
        Task<NotaExamenDto> RegistrarNotaAsync(NotaExamenCrearDto dto);
        Task<IEnumerable<NotaExamenDto>> ObtenerNotasAsync();
        Task<NotaExamenDto?> ObtenerNotaPorIdAsync(int id);
        Task<bool> ActualizarNotaAsync(int id, NotaExamenActualizarDto dto);
        Task<bool> EliminarNotaAsync(int id);
    }
}