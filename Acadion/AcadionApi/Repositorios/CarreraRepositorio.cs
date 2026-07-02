using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Repositorio para operaciones de Carrera
/// </summary>
public class CarreraRepositorio : Repositorio<Carrera>, ICarreraRepositorio
{
    public CarreraRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<Carrera?> GetByNombreAsync(string nombre)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Nombre == nombre);
    }

    public async Task<IEnumerable<Carrera>> GetAllWithAniosAsync()
    {
        return await _dbSet
            .Include(c => c.AniosAcademicos)
            .ToListAsync();
    }

    public async Task<Carrera?> GetWithMateriasAsync(int id)
    {
        return await _dbSet
            .Include(c => c.AniosAcademicos)
                .ThenInclude(a => a.Materias)
            .FirstOrDefaultAsync(c => c.IdCarrera == id);
    }

    public async Task<IEnumerable<Carrera>> GetAllWithAlumnosAsync()
    {
        return await _dbSet
            .Include(c => c.AlumnosInscritos)
            .ToListAsync();
    }
}
