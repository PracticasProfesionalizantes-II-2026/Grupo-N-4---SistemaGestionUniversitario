using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz para operaciones específicas de Asistencia
/// </summary>
public interface IAsistenciaRepositorio : IRepositorio<Asistencia>
{
    Task<IEnumerable<Asistencia>> GetByEstudianteMateriaAsync(int idEstudianteMateria);
    Task<IEnumerable<Asistencia>> GetByDocenteAsync(int idDocente);
    Task<IEnumerable<Asistencia>> GetByFechaAsync(DateTime fecha);
    Task<IEnumerable<Asistencia>> GetByTipoAsync(string tipo);
    Task<IEnumerable<Asistencia>> GetAsistenciasPorEstudianteAsync(int idEstudiante);
    Task<int> ContarAsistenciasAsync(int idEstudianteMateria, string tipo);
}
