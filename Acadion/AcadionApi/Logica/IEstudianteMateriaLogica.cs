using System.Collections.Generic;
using System.Threading.Tasks;
using AcadionApi.DTOs;

namespace AcadionApi.Logica
{
    public interface IEstudianteMateriaLogica
    {
        Task<InscripcionDto> InscribirEstudianteAsync(InscripcionCrearDto dto);
        Task<IEnumerable<InscripcionDto>> ObtenerInscripcionesAsync();
        Task<InscripcionDto?> ObtenerInscripcionPorIdAsync(int id);
        Task<bool> ActualizarInscripcionAsync(int id, InscripcionActualizarDto dto);
        Task<bool> CancelarInscripcionAsync(int id);
    }
}