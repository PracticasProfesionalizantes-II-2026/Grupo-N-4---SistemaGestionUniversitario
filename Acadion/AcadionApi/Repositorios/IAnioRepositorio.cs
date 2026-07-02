using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz para operaciones específicas de Anio
/// </summary>
public interface IAnioRepositorio : IRepositorio<Anio>
{
    Task<Anio?> GetByNumeroAsync(int numero);
    Task<IEnumerable<Anio>> GetByCarreraAsync(int idCarrera);
    Task<Anio?> GetWithMateriasAsync(int id);
    Task<Anio?> GetWithCarreraAsync(int id);
}
