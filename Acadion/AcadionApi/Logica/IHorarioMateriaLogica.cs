using System.Collections.Generic;
using System.Threading.Tasks;
using AcadionApi.DTOs;

namespace AcadionApi.Logica
{
    public interface IHorarioMateriaLogica
    {
        Task<HorarioMateriaDto> RegistrarHorarioAsync(HorarioMateriaCrearDto dto);
        Task<IEnumerable<HorarioMateriaDto>> ObtenerHorariosAsync();
        Task<HorarioMateriaDto?> ObtenerHorarioPorIdAsync(int id);
        Task<bool> ActualizarHorarioAsync(int id, HorarioMateriaActualizarDto dto);
        Task<bool> EliminarHorarioAsync(int id);
    }
}