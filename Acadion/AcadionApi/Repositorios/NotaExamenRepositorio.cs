using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Repositorio para operaciones de NotaExamen
/// </summary>
public class NotaExamenRepositorio : Repositorio<NotaExamen>, INotaExamenRepositorio
{
    public NotaExamenRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<NotaExamen>> GetByExamenAsync(int idExamen)
    {
        return await _dbSet
            .Include(n => n.Examen)
            .Include(n => n.Estudiante)
            .Where(n => n.IdExamen == idExamen)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotaExamen>> GetByEstudianteAsync(int idEstudiante)
{
    return await _dbSet
        .Include(n => n.Examen)
            // Agregamos el signo '!' después de 'e.Examen' si viniera de ahí, 
            // o en este caso indicamos que 'e' (que representa al Examen) no será nulo al acceder a Materia.
            .ThenInclude(e => e!.Materia) 
        .Where(n => n.IdEstudiante == idEstudiante)
        .ToListAsync();
}

    public async Task<NotaExamen?> GetNotaEstudianteExamenAsync(int idExamen, int idEstudiante)
    {
        return await _dbSet
            .Include(n => n.Examen)
            .Include(n => n.Estudiante)
            .FirstOrDefaultAsync(n => n.IdExamen == idExamen && n.IdEstudiante == idEstudiante);
    }

    public async Task<IEnumerable<NotaExamen>> GetNotasAprobatorasAsync()
    {
        return await _dbSet
            .Where(n => n.Nota >= 4)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotaExamen>> GetNotasDesaprobatorasAsync()
    {
        return await _dbSet
            .Where(n => n.Nota < 4)
            .ToListAsync();
    }

    public async Task<decimal> GetPromedioEstudianteAsync(int idEstudiante)
    {
        var notas = await _dbSet
            .Where(n => n.IdEstudiante == idEstudiante)
            .Select(n => n.Nota)
            .ToListAsync();

        return notas.Count > 0 ? notas.Average() : 0;
    }

    public async Task<decimal> GetNotaMaximaExamenAsync(int idExamen)
    {
        var notaMaxima = await _dbSet
            .Where(n => n.IdExamen == idExamen)
            .MaxAsync(n => (decimal?)n.Nota);

        return notaMaxima ?? 0;
    }

    public async Task<decimal> GetNotaMinimaExamenAsync(int idExamen)
    {
        var notaMinima = await _dbSet
            .Where(n => n.IdExamen == idExamen)
            .MinAsync(n => (decimal?)n.Nota);

        return notaMinima ?? 0;
    }
}
