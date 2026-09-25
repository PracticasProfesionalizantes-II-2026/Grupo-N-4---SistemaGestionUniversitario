using AcadionApi.Datos;
using AcadionApi.DTOs;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class PagosEndpoints
{
    public static void MapPagosEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/gestion/pagos")
            .RequierePermiso(PermisosSistema.UsuariosGestionar);

        group.MapGet("/", async (int? anio, int? mes, AppDbContext context) =>
        {
            var periodoAnio = anio ?? DateTime.UtcNow.Year;
            var periodoMes = mes ?? DateTime.UtcNow.Month;
            if (periodoMes is < 1 or > 12) return Results.BadRequest(new { mensaje = "El mes no es válido." });

            var estudiantes = await context.Usuarios.AsNoTracking()
                .Where(u => u.RolId == RolesSistema.EstudianteId)
                .OrderBy(u => u.Persona.Apellido).ThenBy(u => u.Persona.Nombre)
                .Select(u => new
                {
                    u.Id,
                    u.Legajo,
                    Estudiante = u.Persona.Apellido + ", " + u.Persona.Nombre,
                    u.FotoPerfilUrl,
                    EstadoMatricula = context.MatriculasIniciales
                        .Where(m => m.EstudianteId == u.Id)
                        .OrderByDescending(m => m.PeriodoLectivo)
                        .Select(m => m.Estado.ToString())
                        .FirstOrDefault(),
                    EstadoCuota = context.CuotasMensuales
                        .Where(c => c.EstudianteId == u.Id && c.Anio == periodoAnio && c.Mes == periodoMes)
                        .Select(c => c.Estado.ToString())
                        .FirstOrDefault()
                }).ToListAsync();

            return Results.Ok(estudiantes.Select(e => new
            {
                e.Id,
                e.Legajo,
                e.Estudiante,
                e.FotoPerfilUrl,
                EstadoMatricula = e.EstadoMatricula ?? EstadoPago.Pendiente.ToString(),
                EstadoCuota = e.EstadoCuota ?? EstadoPago.Pendiente.ToString(),
                Anio = periodoAnio,
                Mes = periodoMes
            }));
        });

        group.MapPut("/{estudianteId:int}/matricula", async (int estudianteId,
            EstadoPagoActualizarDto dto, AppDbContext context) =>
        {
            if (!await EsEstudianteAsync(estudianteId, context)) return Results.NotFound();
            var estado = ParsearEstado(dto.Estado);
            var matricula = await context.MatriculasIniciales
                .OrderByDescending(m => m.PeriodoLectivo)
                .FirstOrDefaultAsync(m => m.EstudianteId == estudianteId);
            if (matricula is null)
            {
                matricula = new MatriculaInicial
                {
                    EstudianteId = estudianteId,
                    PeriodoLectivo = DateTime.UtcNow.Year
                };
                context.MatriculasIniciales.Add(matricula);
            }
            matricula.Estado = estado;
            matricula.FechaPago = estado == EstadoPago.AlDia ? dto.FechaPago ?? DateTime.UtcNow : null;
            matricula.ComprobanteUrl = dto.ComprobanteUrl?.Trim() ?? string.Empty;
            await context.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPut("/{estudianteId:int}/cuotas/{anio:int}/{mes:int}", async (
            int estudianteId, int anio, int mes, EstadoPagoActualizarDto dto, AppDbContext context) =>
        {
            if (mes is < 1 or > 12) return Results.BadRequest(new { mensaje = "El mes no es válido." });
            if (!await EsEstudianteAsync(estudianteId, context)) return Results.NotFound();
            var estado = ParsearEstado(dto.Estado);
            var cuota = await context.CuotasMensuales.SingleOrDefaultAsync(c =>
                c.EstudianteId == estudianteId && c.Anio == anio && c.Mes == mes);
            if (cuota is null)
            {
                cuota = new CuotaMensual
                {
                    EstudianteId = estudianteId,
                    Anio = anio,
                    Mes = mes,
                    FechaVencimiento = new DateTime(anio, mes, 10, 23, 59, 59, DateTimeKind.Utc)
                };
                context.CuotasMensuales.Add(cuota);
            }
            cuota.Estado = estado;
            cuota.FechaPago = estado == EstadoPago.AlDia ? dto.FechaPago ?? DateTime.UtcNow : null;
            cuota.ComprobanteUrl = dto.ComprobanteUrl?.Trim() ?? string.Empty;
            await context.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static Task<bool> EsEstudianteAsync(int usuarioId, AppDbContext context) =>
        context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.RolId == RolesSistema.EstudianteId);

    private static EstadoPago ParsearEstado(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "PAGADA" or "AL_DIA" or "ALDIA" => EstadoPago.AlDia,
            "EXENTADO" => EstadoPago.Exentado,
            "VENCIDA" => EstadoPago.Vencida,
            "IMPAGA" => EstadoPago.Impaga,
            "PENDIENTE" or "" => EstadoPago.Pendiente,
            _ => throw new BadHttpRequestException("El estado de pago no es válido.")
        };
}
