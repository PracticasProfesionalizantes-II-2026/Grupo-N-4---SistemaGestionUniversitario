using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Repositorio para operaciones de Asistencia
/// </summary>
public class AsistenciaRepositorio : Repositorio<Asistencia>, IAsistenciaRepositorio
{
    public AsistenciaRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Asistencia>> GetByEstudianteMateriaAsync(int idEstudianteMateria)
    {
        return await _dbSet
            .Include(a => a.Inscripcion)
            .Include(a => a.Docente)
            .Where(a => a.IdEstudianteMateria == idEstudianteMateria)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asistencia>> GetByDocenteAsync(int idDocente)
    {
        return await _dbSet
            .Include(a => a.Docente)
            .Include(a => a.Inscripcion)
            .Where(a => a.IdDocente == idDocente)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asistencia>> GetByFechaAsync(DateTime fecha)
    {
        return await _dbSet
            .Where(a => a.Fecha.Date == fecha.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asistencia>> GetByTipoAsync(string tipo)
    {
        return await _dbSet
            .Where(a => a.Tipo == tipo)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asistencia>> GetAsistenciasPorEstudianteAsync(int idEstudiante)
    {
        return await _dbSet
            .Include(a => a.Inscripcion)
            .Include(a => a.Docente)
            .Where(a => a.Inscripcion!.IdEstudiante == idEstudiante)
            .ToListAsync();
    }

    public async Task<int> ContarAsistenciasAsync(int idEstudianteMateria, string tipo)
    {
        return await _dbSet
            .Where(a => a.IdEstudianteMateria == idEstudianteMateria && a.Tipo == tipo)
            .CountAsync();
    }
}
