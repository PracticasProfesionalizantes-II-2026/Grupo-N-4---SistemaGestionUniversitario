using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Repositorio para operaciones de Materia
/// </summary>
public class MateriaRepositorio : Repositorio<Materia>, IMateriaRepositorio
{
    public MateriaRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<Materia?> GetByNombreAsync(string nombre)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.Nombre == nombre);
    }

    public async Task<IEnumerable<Materia>> GetByAnioAsync(int idAnio)
    {
        return await _dbSet
            .Where(m => m.IdAnio == idAnio)
            .ToListAsync();
    }

    public async Task<Materia?> GetWithHorariosAsync(int id)
    {
        return await _dbSet
            .Include(m => m.Horarios)
            .FirstOrDefaultAsync(m => m.IdMateria == id);
    }

    public async Task<Materia?> GetWithCorrelativasAsync(int id)
    {
        return await _dbSet
            .Include(m => m.Correlativas)
            .FirstOrDefaultAsync(m => m.IdMateria == id);
    }

    public async Task<IEnumerable<Materia>> GetByModalidadAsync(string modalidad)
    {
        return await _dbSet
            .Where(m => m.Modalidad == modalidad)
            .ToListAsync();
    }

    public async Task<IEnumerable<Materia>> GetActivasAsync()
    {
        return await _dbSet
            .Where(m => m.Estado == "Activa")
            .ToListAsync();
    }
}
