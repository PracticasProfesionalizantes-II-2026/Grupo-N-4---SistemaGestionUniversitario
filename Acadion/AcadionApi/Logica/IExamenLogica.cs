using System.Collections.Generic;
using System.Threading.Tasks;
using AcadionApi.DTOs;

namespace AcadionApi.Logica
{
    public interface IExamenLogica
    {
        Task<ExamenDto> RegistrarExamenAsync(ExamenCrearDto dto);
        Task<IEnumerable<ExamenDto>> ObtenerExamenesAsync();
        Task<ExamenDto?> ObtenerExamenPorIdAsync(int id);
        Task<bool> ActualizarExamenAsync(int id, ExamenActualizarDto dto);
        Task<bool> EliminarExamenAsync(int id);
    }
}