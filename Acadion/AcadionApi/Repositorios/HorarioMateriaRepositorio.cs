using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Repositorio para operaciones de HorarioMateria
/// </summary>
public class HorarioMateriaRepositorio : Repositorio<HorarioMateria>, IHorarioMateriaRepositorio
{
    public HorarioMateriaRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<HorarioMateria>> GetByMateriaAsync(int idMateria)
    {
        return await _dbSet
            .Where(h => h.IdMateria == idMateria)
            .ToListAsync();
    }

    public async Task<IEnumerable<HorarioMateria>> GetByDiaSemanaAsync(string diaSemana)
    {
        return await _dbSet
            .Where(h => h.DiaSemana == diaSemana)
            .ToListAsync();
    }

    public async Task<IEnumerable<HorarioMateria>> GetHorariosMateriaAsync(int idMateria, string diaSemana)
    {
        return await _dbSet
            .Where(h => h.IdMateria == idMateria && h.DiaSemana == diaSemana)
            .ToListAsync();
    }
}
