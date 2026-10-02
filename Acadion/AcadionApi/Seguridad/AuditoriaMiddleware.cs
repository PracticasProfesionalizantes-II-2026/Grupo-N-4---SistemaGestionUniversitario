using System.Security.Claims;
using AcadionApi.Datos;

namespace AcadionApi.Seguridad;

public sealed class AuditoriaMiddleware
{
    private static readonly HashSet<string> MetodosAuditables =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditoriaMiddleware> _logger;

    public AuditoriaMiddleware(RequestDelegate next, ILogger<AuditoriaMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext http, IServiceScopeFactory scopeFactory)
    {
        Exception? errorEjecucion = null;
        try
        {
            await _next(http);
        }
        catch (Exception error)
        {
            errorEjecucion = error;
        }

        if (!MetodosAuditables.Contains(http.Request.Method) ||
            http.Request.Path.StartsWithSegments("/api/auth/login") ||
            http.Request.Path.StartsWithSegments("/api/auth/recuperacion") ||
            http.User.Identity?.IsAuthenticated != true)
        {
            if (errorEjecucion is not null) throw errorEjecucion;
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var idClaim = http.User.FindFirstValue(ClaimsSistema.UsuarioId);
            context.Set<RegistroAuditoria>().Add(new RegistroAuditoria
            {
                UsuarioId = int.TryParse(idClaim, out var usuarioId) ? usuarioId : null,
                NombreUsuario = http.User.Identity?.Name ?? string.Empty,
                Rol = http.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
                Metodo = http.Request.Method,
                Ruta = http.Request.Path.Value ?? string.Empty,
                EstadoHttp = errorEjecucion is null ? http.Response.StatusCode : 500,
                Ip = http.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                Detalle = errorEjecucion is null && http.Response.StatusCode is >= 200 and < 400
                    ? "Operación completada"
                    : errorEjecucion is null ? "Operación rechazada o fallida" : "Error no controlado durante la operación",
                FechaUtc = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
        catch (Exception error)
        {
            _logger.LogError(error, "No fue posible registrar la auditoría de {Metodo} {Ruta}.",
                http.Request.Method, http.Request.Path);
        }

        if (errorEjecucion is not null) throw errorEjecucion;
    }
}
