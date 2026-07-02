using System.Collections.Generic;
using System.Threading.Tasks;
using AcadionApi.DTOs;

namespace AcadionApi.Logica
{
    public interface IUsuarioLogica
    {
        // Retorna el DTO de respuesta y puede lanzar excepciones controladas para los códigos 409, 400, etc.
        Task<UsuarioDto> RegistrarUsuarioAsync(UsuarioCrearDto dto);

        Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
        
        Task<IEnumerable<UsuarioListaDto>> ObtenerUsuariosAsync();
        
        Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(int id);
        
        Task<bool> ActualizarUsuarioAsync(int id, UsuarioActualizarDto dto);
        
        Task<bool> EliminarUsuarioAsync(int id);
    }
}
