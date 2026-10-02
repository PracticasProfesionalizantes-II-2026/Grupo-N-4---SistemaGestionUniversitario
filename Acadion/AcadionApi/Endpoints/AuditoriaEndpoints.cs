using AcadionApi.Datos;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class AuditoriaEndpoints
{
    public static void MapAuditoriaEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/auditoria", async (DateTime? desde, DateTime? hasta,
            string? usuario, string? accion, int pagina, int cantidad, AppDbContext context) =>
        {
            pagina = Math.Max(1, pagina);
            cantidad = Math.Clamp(cantidad <= 0 ? 50 : cantidad, 1, 100);
            var consulta = context.Set<RegistroAuditoria>().AsNoTracking().AsQueryable();
            if (desde.HasValue) consulta = consulta.Where(r => r.FechaUtc >= desde.Value.ToUniversalTime());
            if (hasta.HasValue) consulta = consulta.Where(r => r.FechaUtc <= hasta.Value.ToUniversalTime());
            if (!string.IsNullOrWhiteSpace(usuario))
                consulta = consulta.Where(r => r.NombreUsuario.Contains(usuario.Trim()));
            if (!string.IsNullOrWhiteSpace(accion))
                consulta = consulta.Where(r => r.Ruta.Contains(accion.Trim()) || r.Detalle.Contains(accion.Trim()));

            var total = await consulta.CountAsync();
            var registros = await consulta.OrderByDescending(r => r.FechaUtc)
                .Skip((pagina - 1) * cantidad).Take(cantidad).ToListAsync();
            return Results.Ok(new
            {
                total, pagina, cantidad,
                registros = registros.Select(r => new
                {
                    r.Id, r.UsuarioId, r.NombreUsuario, r.Rol, r.Metodo, r.Ruta,
                    r.EstadoHttp, r.Detalle, r.FechaUtc,
                    Accion = DescribirAccion(r.Metodo, r.Ruta, r.Detalle),
                    Resultado = r.EstadoHttp is >= 200 and < 400 ? "Correcto" : "Fallido"
                })
            });
        }).RequireAuthorization(policy =>
        {
            policy.RequireRole(RolesSistema.Secretario);
            policy.RequireClaim(ClaimsSistema.Permiso, PermisosSistema.UsuariosGestionar);
        });
    }

    private static string DescribirAccion(string metodo, string ruta, string detalle)
    {
        if (ruta.Contains("/auth/logout", StringComparison.OrdinalIgnoreCase)) return "Cerró sesión";
        if (ruta.Contains("/notas", StringComparison.OrdinalIgnoreCase)) return "Modificó una nota";
        if (ruta.Contains("justificacion", StringComparison.OrdinalIgnoreCase)) return "Gestionó una justificación";
        if (ruta.Contains("/pagos", StringComparison.OrdinalIgnoreCase)) return "Cambió o envió un pago";
        if (ruta.Contains("/usuarios", StringComparison.OrdinalIgnoreCase)) return "Editó un usuario";
        if (ruta.Contains("/materias", StringComparison.OrdinalIgnoreCase)) return "Editó una materia";
        if (ruta.Contains("/carreras", StringComparison.OrdinalIgnoreCase)) return "Editó una carrera";
        if (ruta.Contains("/asistencias", StringComparison.OrdinalIgnoreCase)) return "Registró una asistencia";
        if (ruta.Contains("/examenes", StringComparison.OrdinalIgnoreCase)) return "Gestionó una evaluación";
        if (ruta.Contains("/inscripciones", StringComparison.OrdinalIgnoreCase)) return "Gestionó una inscripción";
        if (ruta.Contains("/calendario", StringComparison.OrdinalIgnoreCase)) return "Editó el calendario académico";
        if (ruta.Contains("/notificaciones", StringComparison.OrdinalIgnoreCase)) return "Gestionó una notificación";
        if (ruta.Contains("/perfil", StringComparison.OrdinalIgnoreCase)) return "Actualizó un perfil";
        return metodo switch
        {
            "POST" => "Registró información",
            "PUT" or "PATCH" => "Actualizó información",
            "DELETE" => "Desactivó un registro",
            _ => string.IsNullOrWhiteSpace(detalle) ? "Consultó información" : detalle
        };
    }
}
