using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz para operaciones específicas de EstudianteMateria
/// </summary>
public interface IEstudianteMateriaRepositorio : IRepositorio<EstudianteMateria>
{
    Task<IEnumerable<EstudianteMateria>> GetByEstudianteAsync(int idEstudiante);
    Task<IEnumerable<EstudianteMateria>> GetByMateriaAsync(int idMateria);
    Task<IEnumerable<EstudianteMateria>> GetByDocenteAsync(int idDocente);
    Task<EstudianteMateria?> GetInscripcionAsync(int idEstudiante, int idMateria);
    Task<IEnumerable<EstudianteMateria>> GetByCicloLectivoAsync(int cicloLectivo);
    Task<IEnumerable<EstudianteMateria>> GetByEstadoAsync(string estado);
    Task<IEnumerable<EstudianteMateria>> GetWithInformacionCompletaAsync(int idEstudiante);
}
