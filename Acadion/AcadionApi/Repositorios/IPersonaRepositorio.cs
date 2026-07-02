using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz para operaciones específicas de Persona
/// </summary>
public interface IPersonaRepositorio : IRepositorio<Persona>
{
    Task<Persona?> GetByDniAsync(long dni);
    Task<Persona?> GetByEmailAsync(string email);
    Task<IEnumerable<Persona>> GetByApellidoAsync(string apellido);
    Task<IEnumerable<Persona>> GetByLocalidadAsync(string localidad);
}
