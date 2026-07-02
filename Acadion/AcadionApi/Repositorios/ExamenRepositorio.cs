using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Repositorio para operaciones de Examen
/// </summary>
public class ExamenRepositorio : Repositorio<Examen>, IExamenRepositorio
{
    public ExamenRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Examen>> GetByMateriaAsync(int idMateria)
    {
        return await _dbSet
            .Include(e => e.Materia)
            .Include(e => e.Docente)
            .Where(e => e.IdMateria == idMateria)
            .ToListAsync();
    }

    public async Task<IEnumerable<Examen>> GetByDocenteAsync(int idDocente)
    {
        return await _dbSet
            .Include(e => e.Materia)
            .Where(e => e.IdDocente == idDocente)
            .ToListAsync();
    }

    public async Task<IEnumerable<Examen>> GetByFechaAsync(DateTime fecha)
    {
        return await _dbSet
            .Where(e => e.Fecha.Date == fecha.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Examen>> GetByTipoAsync(string tipoExamen)
    {
        return await _dbSet
            .Where(e => e.TipoExamen == tipoExamen)
            .ToListAsync();
    }

    public async Task<IEnumerable<Examen>> GetByCicloLectivoAsync(int cicloLectivo)
    {
        return await _dbSet
            .Where(e => e.CicloLectivo == cicloLectivo)
            .ToListAsync();
    }

    public async Task<Examen?> GetWithNotasAsync(int id)
    {
        return await _dbSet
            .Include(e => e.Notas)
            .Include(e => e.Materia)
            .Include(e => e.Docente)
            .FirstOrDefaultAsync(e => e.IdExamen == id);
    }

    public async Task<IEnumerable<Examen>> GetProximosExamenesAsync()
    {
        return await _dbSet
            .Where(e => e.Fecha >= DateTime.Now)
            .OrderBy(e => e.Fecha)
            .ToListAsync();
    }
}
