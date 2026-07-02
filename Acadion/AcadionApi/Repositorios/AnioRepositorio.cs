using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Repositorio para operaciones de Anio
/// </summary>
public class AnioRepositorio : Repositorio<Anio>, IAnioRepositorio
{
    public AnioRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<Anio?> GetByNumeroAsync(int numero)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.NumeroAnio == numero);
    }

    public async Task<IEnumerable<Anio>> GetByCarreraAsync(int idCarrera)
    {
        return await _dbSet
            .Where(a => a.IdCarrera == idCarrera)
            .ToListAsync();
    }

    public async Task<Anio?> GetWithMateriasAsync(int id)
    {
        return await _dbSet
            .Include(a => a.Materias)
            .FirstOrDefaultAsync(a => a.IdAnio == id);
    }

    public async Task<Anio?> GetWithCarreraAsync(int id)
    {
        return await _dbSet
            .Include(a => a.Carrera)
            .FirstOrDefaultAsync(a => a.IdAnio == id);
    }
}
