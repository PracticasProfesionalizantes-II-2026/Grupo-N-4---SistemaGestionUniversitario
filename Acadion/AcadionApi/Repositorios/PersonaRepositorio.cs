using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Repositorio para operaciones de Persona
/// </summary>
using Microsoft.EntityFrameworkCore;

public class PersonaRepositorio : Repositorio<Persona>, IPersonaRepositorio
{
    public PersonaRepositorio(AppDbContext context) : base(context)
    {
    }

    public async Task<Persona?> GetByDniAsync(long dni)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Dni == dni);
    }

    public async Task<Persona?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Email == email);
    }

    public async Task<IEnumerable<Persona>> GetByApellidoAsync(string apellido)
    {
        return await _dbSet
            .Where(p => p.Apellido.Contains(apellido))
            .ToListAsync();
    }

    public async Task<IEnumerable<Persona>> GetByLocalidadAsync(string localidad)
    {
        return await _dbSet
            .Where(p => p.Localidad == localidad)
            .ToListAsync();
    }

    // NUEVO
    public async Task<IEnumerable<Persona>> GetByFechaNacimientoAsync(DateTime fecha)
    {
        return await _dbSet
            .Where(p => p.FechaNacimiento.Date == fecha.Date)
            .ToListAsync();
    }

    // NUEVO
    public async Task<IEnumerable<Persona>> GetMayoresDeEdadAsync()
    {
        return await _dbSet
            .Where(p =>
                DateTime.Today.Year - p.FechaNacimiento.Year >= 18)
            .ToListAsync();
    }
}