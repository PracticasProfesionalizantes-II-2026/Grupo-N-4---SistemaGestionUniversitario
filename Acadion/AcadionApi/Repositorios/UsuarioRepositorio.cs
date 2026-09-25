using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Repositorio para operaciones de Usuario
/// </summary>
public class UsuarioRepositorio : Repositorio<Usuario>, IUsuarioRepositorio
{
    public UsuarioRepositorio(AppDbContext context) : base(context)
    {
    }
    
    public override async Task<IEnumerable<Usuario>> GetAllAsync()
    {
    return await _context.Usuarios
        .Include(u => u.Persona)
        .Include(u => u.Rol)
        .ToListAsync();
    }
    public override async Task<Usuario?> GetByIdAsync(int id)
    {
    return await _context.Usuarios
        .Include(u => u.Persona)
        .Include(u => u.Rol)
        .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario)
    {
        return await _context.Usuarios
            .Include(u => u.Persona)
            .Include(u => u.Rol)
                .ThenInclude(r => r.RolPermisos)
                    .ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u =>
                u.NombreUsuario == nombreUsuario);
    }

    public async Task<Usuario?> GetByEmailInstitucionalAsync(string email)
    {
        return await _context.Usuarios.FirstOrDefaultAsync(u => u.EmailInstitucional == email);
    }

    public async Task<IEnumerable<Usuario>> GetByRolAsync(int rolId)
    {
        return await _context.Usuarios
            .Include(u => u.Persona)
            .Where(u => u.RolId == rolId)
            .OrderBy(u => u.Persona.Apellido)
            .ThenBy(u => u.Persona.Nombre)
            .ToListAsync();
    }

    public async Task<IEnumerable<Usuario>> GetByEstadoAsync(EstadoUsuario estado)
    {
        return await _context.Usuarios.Where(u => u.Estado == estado).ToListAsync();
    }

    public async Task<IEnumerable<Usuario>> GetEstudiantesAsync()
    {
        return await GetByRolAsync(AcadionApi.Seguridad.RolesSistema.EstudianteId);
    }

    public async Task<IEnumerable<Usuario>> GetDocentesAsync()
    {
        return await GetByRolAsync(AcadionApi.Seguridad.RolesSistema.DocenteId);
    }

    public async Task<IEnumerable<Usuario>> GetActivosAsync()
    {
        return await GetByEstadoAsync(EstadoUsuario.Activo);
    }

    public async Task<Usuario?> ValidarCredencialesAsync(string nombreUsuario, string password)
    {
        return await _context.Usuarios.FirstOrDefaultAsync(u => 
            u.NombreUsuario == nombreUsuario && u.PasswordHash == password);
    }
}
