using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz para operaciones específicas de Usuario
/// </summary>
public interface IUsuarioRepositorio : IRepositorio<Usuario>
{
    Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
    Task<Usuario?> GetByEmailInstitucionalAsync(string email);
    Task<IEnumerable<Usuario>> GetByRolAsync(Rol rol);
    Task<IEnumerable<Usuario>> GetByEstadoAsync(EstadoUsuario estado);
    Task<IEnumerable<Usuario>> GetEstudiantesAsync();
    Task<IEnumerable<Usuario>> GetDocentesAsync();
    Task<IEnumerable<Usuario>> GetActivosAsync();
    Task<Usuario?> ValidarCredencialesAsync(string nombreUsuario, string Password);
    
}
