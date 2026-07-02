using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Repositorio para operaciones de EstudianteMateria
/// </summary>
public class EstudianteMateriaRepositorio : Repositorio<EstudianteMateria>, IEstudianteMateriaRepositorio
{
    public EstudianteMateriaRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<EstudianteMateria>> GetByEstudianteAsync(int idEstudiante)
    {
        return await _dbSet
            .Include(em => em.Materia)
            .Include(em => em.Docente)
            .Where(em => em.IdEstudiante == idEstudiante)
            .ToListAsync();
    }

    public async Task<IEnumerable<EstudianteMateria>> GetByMateriaAsync(int idMateria)
    {
        return await _dbSet
            .Include(em => em.Estudiante)
            .Where(em => em.IdMateria == idMateria)
            .ToListAsync();
    }

    public async Task<IEnumerable<EstudianteMateria>> GetByDocenteAsync(int idDocente)
    {
        return await _dbSet
            .Include(em => em.Estudiante)
            .Include(em => em.Materia)
            .Where(em => em.IdDocente == idDocente)
            .ToListAsync();
    }

    public async Task<EstudianteMateria?> GetInscripcionAsync(int idEstudiante, int idMateria)
    {
        return await _dbSet
            .Include(em => em.Materia)
            .Include(em => em.Docente)
            .FirstOrDefaultAsync(em => em.IdEstudiante == idEstudiante && em.IdMateria == idMateria);
    }

    public async Task<IEnumerable<EstudianteMateria>> GetByCicloLectivoAsync(int cicloLectivo)
    {
        return await _dbSet
            .Where(em => em.CicloLectivo == cicloLectivo)
            .ToListAsync();
    }

    public async Task<IEnumerable<EstudianteMateria>> GetByEstadoAsync(string estado)
    {
        return await _dbSet
            .Where(em => em.Estado == estado)
            .ToListAsync();
    }

    public async Task<IEnumerable<EstudianteMateria>> GetWithInformacionCompletaAsync(int idEstudiante)
    {
        return await _dbSet
            .Include(em => em.Estudiante)
            .Include(em => em.Materia)
            .Include(em => em.Docente)
            .Where(em => em.IdEstudiante == idEstudiante)
            .ToListAsync();
    }
}
