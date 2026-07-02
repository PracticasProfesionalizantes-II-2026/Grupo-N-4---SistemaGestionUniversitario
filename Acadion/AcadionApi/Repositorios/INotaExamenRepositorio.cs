using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz para operaciones específicas de NotaExamen
/// </summary>
public interface INotaExamenRepositorio : IRepositorio<NotaExamen>
{
    Task<IEnumerable<NotaExamen>> GetByExamenAsync(int idExamen);
    Task<IEnumerable<NotaExamen>> GetByEstudianteAsync(int idEstudiante);
    Task<NotaExamen?> GetNotaEstudianteExamenAsync(int idExamen, int idEstudiante);
    Task<IEnumerable<NotaExamen>> GetNotasAprobatorasAsync();
    Task<IEnumerable<NotaExamen>> GetNotasDesaprobatorasAsync();
    Task<decimal> GetPromedioEstudianteAsync(int idEstudiante);
    Task<decimal> GetNotaMaximaExamenAsync(int idExamen);
    Task<decimal> GetNotaMinimaExamenAsync(int idExamen);
}
