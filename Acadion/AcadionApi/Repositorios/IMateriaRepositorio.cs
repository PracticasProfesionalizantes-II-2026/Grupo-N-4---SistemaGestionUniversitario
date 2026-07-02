using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz para operaciones específicas de Materia
/// </summary>
public interface IMateriaRepositorio : IRepositorio<Materia>
{
    Task<Materia?> GetByNombreAsync(string nombre);
    Task<IEnumerable<Materia>> GetByAnioAsync(int idAnio);
    Task<Materia?> GetWithHorariosAsync(int id);
    Task<Materia?> GetWithCorrelativasAsync(int id);
    Task<IEnumerable<Materia>> GetByModalidadAsync(string modalidad);
    Task<IEnumerable<Materia>> GetActivasAsync();
}
