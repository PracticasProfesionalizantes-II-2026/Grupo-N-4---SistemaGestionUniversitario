using System.Collections.Generic;
using System.Threading.Tasks;
using AcadionApi.DTOs;

namespace AcadionApi.Logica
{
    public interface IPersonaLogica
    {
        Task<PersonaDto> RegistrarPersonaAsync(PersonaCrearDto dto);
        Task<IEnumerable<PersonaDto>> ObtenerPersonasAsync();
        Task<PersonaDto?> ObtenerPersonaPorIdAsync(int id);
        Task<bool> ActualizarPersonaAsync(int id, PersonaActualizarDto dto);
        Task<bool> EliminarPersonaAsync(int id);
    }
}