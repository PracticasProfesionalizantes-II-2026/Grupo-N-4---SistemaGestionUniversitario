using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz para operaciones específicas de Examen
/// </summary>
public interface IExamenRepositorio : IRepositorio<Examen>
{
    Task<IEnumerable<Examen>> GetByMateriaAsync(int idMateria);
    Task<IEnumerable<Examen>> GetByDocenteAsync(int idDocente);
    Task<IEnumerable<Examen>> GetByFechaAsync(DateTime fecha);
    Task<IEnumerable<Examen>> GetByTipoAsync(string tipoExamen);
    Task<IEnumerable<Examen>> GetByCicloLectivoAsync(int cicloLectivo);
    Task<Examen?> GetWithNotasAsync(int id);
    Task<IEnumerable<Examen>> GetProximosExamenesAsync();
}
