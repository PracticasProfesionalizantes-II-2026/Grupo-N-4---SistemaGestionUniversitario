using AcadionApi.Datos;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class RolesEndpoints
{
    public static void MapRolesEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/roles", async (AppDbContext context) =>
            Results.Ok(await context.Roles.AsNoTracking()
                .Include(r => r.RolPermisos).ThenInclude(rp => rp.Permiso)
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Nombre,
                    r.Descripcion,
                    Permisos = r.RolPermisos.Select(rp => rp.Permiso.Codigo).OrderBy(c => c)
                }).ToListAsync()))
            .RequierePermiso(PermisosSistema.UsuariosLeer);
    }
}
