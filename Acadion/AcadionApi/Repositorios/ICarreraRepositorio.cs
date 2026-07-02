using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz para operaciones específicas de Carrera
/// </summary>
public interface ICarreraRepositorio : IRepositorio<Carrera>
{
    Task<Carrera?> GetByNombreAsync(string nombre);
    Task<IEnumerable<Carrera>> GetAllWithAniosAsync();
    Task<Carrera?> GetWithMateriasAsync(int id);
    Task<IEnumerable<Carrera>> GetAllWithAlumnosAsync();
}
