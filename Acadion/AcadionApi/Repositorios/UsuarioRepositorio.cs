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

    public async Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u =>
                u.NombreUsuario == nombreUsuario);
    }

    public async Task<Usuario?> GetByEmailInstitucionalAsync(string email)
    {
        return await _context.Usuarios.FirstOrDefaultAsync(u => u.EmailInstitucional == email);
    }

    public async Task<IEnumerable<Usuario>> GetByRolAsync(Rol rol)
    {
        return await _context.Usuarios.Where(u => u.Rol == rol).ToListAsync();
    }

    public async Task<IEnumerable<Usuario>> GetByEstadoAsync(EstadoUsuario estado)
    {
        return await _context.Usuarios.Where(u => u.Estado == estado).ToListAsync();
    }

    public async Task<IEnumerable<Usuario>> GetEstudiantesAsync()
    {
        return await GetByRolAsync(Rol.Estudiante);
    }

    public async Task<IEnumerable<Usuario>> GetDocentesAsync()
    {
        return await GetByRolAsync(Rol.Docente);
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
