using AcadionApi.Datos;
using AcadionApi.DTOs;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class NotificacionesEndpoints
{
    public static void MapNotificacionesEndpoints(this IEndpointRouteBuilder routes)
    {
        var gestion = routes.MapGroup("/api/notificaciones")
            .RequierePermiso(PermisosSistema.NotificacionesGestionar);

        gestion.MapGet("/", async (AppDbContext context) =>
        {
            var notificaciones = await ConsultaBase(context)
                .Where(n => n.ClaveAutomatica == null)
                .OrderByDescending(n => n.FechaPublicacionUtc)
                .ToListAsync();
            return Results.Ok(notificaciones.Select(Mapear));
        });

        gestion.MapPost("/", async (NotificacionGeneralCrearDto dto,
            HttpContext http, AppDbContext context) =>
        {
            var error = await ValidarAsync(dto, context);
            if (error is not null) return Results.BadRequest(new { mensaje = error });

            var notificacion = new NotificacionGeneral
            {
                Titulo = dto.Titulo.Trim(),
                Mensaje = dto.Mensaje.Trim(),
                RolDestinoId = dto.RolDestinoId,
                Prioridad = NormalizarPrioridad(dto.Prioridad),
                FechaExpiracionUtc = dto.FechaExpiracionUtc,
                CreadaPorUsuarioId = http.User.ObtenerUsuarioId()
            };
            context.Set<NotificacionGeneral>().Add(notificacion);
            await context.SaveChangesAsync();
            return Results.Created($"/api/notificaciones/{notificacion.Id}", new { notificacion.Id });
        });

        gestion.MapPatch("/{id:int}/estado", async (int id, bool activa, AppDbContext context) =>
        {
            var notificacion = await context.Set<NotificacionGeneral>().FindAsync(id);
            if (notificacion is null) return Results.NotFound();
            notificacion.Activa = activa;
            await context.SaveChangesAsync();
            return Results.NoContent();
        });

        routes.MapGet("/api/me/notificaciones", async (HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var rolId = await context.Usuarios
                .Where(u => u.Id == usuarioId)
                .Select(u => u.RolId)
                .SingleAsync();
            var ahora = DateTime.UtcNow;
            var notificaciones = await ConsultaBase(context)
                .Where(n => n.Activa &&
                    n.FechaPublicacionUtc <= ahora &&
                    (n.FechaExpiracionUtc == null || n.FechaExpiracionUtc >= ahora) &&
                    (n.UsuarioDestinoId == usuarioId ||
                     n.UsuarioDestinoId == null && (n.RolDestinoId == null || n.RolDestinoId == rolId)))
                .OrderByDescending(n => n.FechaPublicacionUtc)
                .ToListAsync();
            return Results.Ok(notificaciones.Select(Mapear));
        }).RequireAuthorization();
    }

    private static IQueryable<NotificacionGeneral> ConsultaBase(AppDbContext context) =>
        context.Set<NotificacionGeneral>()
            .AsNoTracking()
            .Include(n => n.RolDestino)
            .Include(n => n.UsuarioDestino).ThenInclude(u => u!.Persona)
            .Include(n => n.CreadaPor).ThenInclude(u => u!.Persona);

    private static async Task<string?> ValidarAsync(
        NotificacionGeneralCrearDto dto, AppDbContext context)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo)) return "El título es obligatorio.";
        if (string.IsNullOrWhiteSpace(dto.Mensaje)) return "El mensaje es obligatorio.";
        if (dto.FechaExpiracionUtc.HasValue && dto.FechaExpiracionUtc <= DateTime.UtcNow)
            return "La fecha de vencimiento debe ser futura.";
        if (dto.RolDestinoId.HasValue &&
            !await context.Roles.AnyAsync(r => r.Id == dto.RolDestinoId.Value))
            return "El grupo destinatario no existe.";
        return null;
    }

    private static string NormalizarPrioridad(string? prioridad) =>
        prioridad?.Trim().ToLowerInvariant() switch
        {
            "alta" => "Alta",
            "baja" => "Baja",
            _ => "Normal"
        };

    private static NotificacionGeneralDto Mapear(NotificacionGeneral n) => new()
    {
        Id = n.Id,
        Titulo = n.Titulo,
        Mensaje = n.Mensaje,
        RolDestinoId = n.RolDestinoId,
        Destinatarios = n.UsuarioDestino is not null
            ? $"{n.UsuarioDestino.Persona.Nombre} {n.UsuarioDestino.Persona.Apellido}".Trim()
            : n.RolDestino?.Nombre ?? "Todos",
        Prioridad = n.Prioridad,
        FechaPublicacionUtc = n.FechaPublicacionUtc,
        FechaExpiracionUtc = n.FechaExpiracionUtc,
        Activa = n.Activa,
        CreadaPor = n.CreadaPor is null
            ? "Sistema Acadion"
            : $"{n.CreadaPor.Persona.Nombre} {n.CreadaPor.Persona.Apellido}".Trim()
    };
}
