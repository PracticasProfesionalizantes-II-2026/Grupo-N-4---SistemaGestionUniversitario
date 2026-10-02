using AcadionApi.Datos;
using AcadionApi.DTOs;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

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
                    ComprobanteMatricula = context.MatriculasIniciales
                        .Where(m => m.EstudianteId == u.Id && m.PeriodoLectivo == periodoAnio)
                        .Select(m => m.ComprobanteUrl).FirstOrDefault(),
                    EstadoCuota = context.CuotasMensuales
                        .Where(c => c.EstudianteId == u.Id && c.Anio == periodoAnio && c.Mes == periodoMes)
                        .Select(c => c.Estado.ToString())
                        .FirstOrDefault(),
                    ImporteCuota = context.CuotasMensuales
                        .Where(c => c.EstudianteId == u.Id && c.Anio == periodoAnio && c.Mes == periodoMes)
                        .Select(c => (decimal?)c.Importe).FirstOrDefault(),
                    MetodoPagoCuota = context.CuotasMensuales
                        .Where(c => c.EstudianteId == u.Id && c.Anio == periodoAnio && c.Mes == periodoMes)
                        .Select(c => c.MetodoPago).FirstOrDefault(),
                    ComprobanteCuota = context.CuotasMensuales
                        .Where(c => c.EstudianteId == u.Id && c.Anio == periodoAnio && c.Mes == periodoMes)
                        .Select(c => c.ComprobanteUrl).FirstOrDefault(),
                    ObservacionesCuota = context.CuotasMensuales
                        .Where(c => c.EstudianteId == u.Id && c.Anio == periodoAnio && c.Mes == periodoMes)
                        .Select(c => c.Observaciones).FirstOrDefault()
                }).ToListAsync();

            return Results.Ok(estudiantes.Select(e => new
            {
                e.Id,
                e.Legajo,
                e.Estudiante,
                e.FotoPerfilUrl,
                EstadoMatricula = e.EstadoMatricula ?? EstadoPago.Pendiente.ToString(),
                ComprobanteMatricula = e.ComprobanteMatricula ?? string.Empty,
                EstadoCuota = e.EstadoCuota ?? EstadoPago.Pendiente.ToString(),
                ImporteCuota = e.ImporteCuota ?? 0,
                MetodoPagoCuota = e.MetodoPagoCuota ?? string.Empty,
                ComprobanteCuota = e.ComprobanteCuota ?? string.Empty,
                ObservacionesCuota = e.ObservacionesCuota ?? string.Empty,
                Anio = periodoAnio,
                Mes = periodoMes
            }));
        });

        group.MapPut("/{estudianteId:int}/matricula", async (int estudianteId,
            EstadoPagoActualizarDto dto, HttpContext http, AppDbContext context) =>
        {
            if (!await EsEstudianteAsync(estudianteId, context)) return Results.NotFound();
            var error = ValidarDetalle(dto);
            if (error is not null) return Results.BadRequest(new { mensaje = error });
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
            matricula.Importe = dto.Importe;
            matricula.MetodoPago = dto.MetodoPago?.Trim() ?? string.Empty;
            matricula.Observaciones = dto.Observaciones?.Trim() ?? string.Empty;
            matricula.ValidadoPorUsuarioId = http.User.ObtenerUsuarioId();
            matricula.FechaValidacionUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPut("/{estudianteId:int}/cuotas/{anio:int}/{mes:int}", async (
            int estudianteId, int anio, int mes, EstadoPagoActualizarDto dto,
            HttpContext http, AppDbContext context) =>
        {
            if (mes is < 1 or > 12) return Results.BadRequest(new { mensaje = "El mes no es válido." });
            if (!await EsEstudianteAsync(estudianteId, context)) return Results.NotFound();
            var error = ValidarDetalle(dto);
            if (error is not null) return Results.BadRequest(new { mensaje = error });
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
            cuota.Importe = dto.Importe;
            cuota.MetodoPago = dto.MetodoPago?.Trim() ?? string.Empty;
            cuota.Observaciones = dto.Observaciones?.Trim() ?? string.Empty;
            cuota.ValidadoPorUsuarioId = http.User.ObtenerUsuarioId();
            cuota.FechaValidacionUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapGet("/{estudianteId:int}/matricula/{anio:int}/comprobante", async (int estudianteId,
            int anio, AppDbContext context, DocumentoStorage storage) =>
        {
            var ruta = await context.MatriculasIniciales.AsNoTracking()
                .Where(m => m.EstudianteId == estudianteId && m.PeriodoLectivo == anio)
                .Select(m => m.ComprobanteUrl).SingleOrDefaultAsync();
            return Descargar(storage, ruta, $"comprobante-matricula-{anio}");
        });

        group.MapGet("/{estudianteId:int}/cuotas/{anio:int}/{mes:int}/comprobante", async (
            int estudianteId, int anio, int mes, AppDbContext context, DocumentoStorage storage) =>
        {
            var ruta = await context.CuotasMensuales.AsNoTracking()
                .Where(c => c.EstudianteId == estudianteId && c.Anio == anio && c.Mes == mes)
                .Select(c => c.ComprobanteUrl).SingleOrDefaultAsync();
            return Descargar(storage, ruta, $"comprobante-cuota-{anio}-{mes:D2}");
        });

        var personal = routes.MapGroup("/api/me/pagos").RequireAuthorization();
        personal.MapGet("/", async (int? anio, int? mes, HttpContext http, AppDbContext context) =>
        {
            var estudianteId = http.User.ObtenerUsuarioId();
            if (!await EsEstudianteAsync(estudianteId, context)) return Results.Forbid();
            var periodoAnio = anio ?? DateTime.UtcNow.Year;
            var periodoMes = mes ?? DateTime.UtcNow.Month;
            if (periodoMes is < 1 or > 12) return Results.BadRequest(new { mensaje = "El mes no es válido." });
            var matricula = await context.MatriculasIniciales.AsNoTracking()
                .SingleOrDefaultAsync(m => m.EstudianteId == estudianteId && m.PeriodoLectivo == periodoAnio);
            var cuota = await context.CuotasMensuales.AsNoTracking()
                .SingleOrDefaultAsync(c => c.EstudianteId == estudianteId && c.Anio == periodoAnio && c.Mes == periodoMes);
            return Results.Ok(new
            {
                Anio = periodoAnio,
                Mes = periodoMes,
                Matricula = matricula is null ? null : new { Estado = matricula.Estado.ToString(), TieneComprobante = !string.IsNullOrEmpty(matricula.ComprobanteUrl), matricula.Importe, matricula.MetodoPago, matricula.Observaciones },
                Cuota = cuota is null ? null : new { Estado = cuota.Estado.ToString(), TieneComprobante = !string.IsNullOrEmpty(cuota.ComprobanteUrl), cuota.Importe, cuota.MetodoPago, cuota.Observaciones }
            });
        });

        personal.MapPost("/matricula/{anio:int}/comprobante", async (int anio, [FromForm] IFormFile archivo,
            [FromForm] string? metodoPago, [FromForm] decimal? importe, HttpContext http, AppDbContext context,
            DocumentoStorage storage, CancellationToken cancellationToken) =>
        {
            var estudianteId = http.User.ObtenerUsuarioId();
            if (!await EsEstudianteAsync(estudianteId, context)) return Results.Forbid();
            if (anio is < 2020 or > 2100) return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });
            var errorDetalle = ValidarCarga(metodoPago, importe);
            if (errorDetalle is not null) return Results.BadRequest(new { mensaje = errorDetalle });
            try
            {
                var ruta = await storage.GuardarAsync(archivo, $"pagos/{estudianteId}", cancellationToken);
                var matricula = await context.MatriculasIniciales.SingleOrDefaultAsync(m => m.EstudianteId == estudianteId && m.PeriodoLectivo == anio);
                if (matricula is null)
                {
                    matricula = new MatriculaInicial { EstudianteId = estudianteId, PeriodoLectivo = anio };
                    context.MatriculasIniciales.Add(matricula);
                }
                matricula.ComprobanteUrl = ruta;
                matricula.Estado = EstadoPago.EnRevision;
                matricula.Importe = importe ?? 0;
                matricula.MetodoPago = (metodoPago ?? string.Empty).Trim();
                matricula.ValidadoPorUsuarioId = null;
                matricula.FechaValidacionUtc = null;
                await context.SaveChangesAsync();
                return Results.Ok(new { mensaje = "El comprobante de matrícula quedó pendiente de revisión." });
            }
            catch (ArgumentException error) { return Results.BadRequest(new { mensaje = error.Message }); }
        }).DisableAntiforgery();

        personal.MapPost("/cuotas/{anio:int}/{mes:int}/comprobante", async (int anio, int mes,
            [FromForm] IFormFile archivo, [FromForm] string? metodoPago, [FromForm] decimal? importe, HttpContext http,
            AppDbContext context, DocumentoStorage storage, CancellationToken cancellationToken) =>
        {
            var estudianteId = http.User.ObtenerUsuarioId();
            if (!await EsEstudianteAsync(estudianteId, context)) return Results.Forbid();
            if (mes is < 1 or > 12 || anio is < 2020 or > 2100) return Results.BadRequest(new { mensaje = "El período no es válido." });
            var errorDetalle = ValidarCarga(metodoPago, importe);
            if (errorDetalle is not null) return Results.BadRequest(new { mensaje = errorDetalle });
            try
            {
                var ruta = await storage.GuardarAsync(archivo, $"pagos/{estudianteId}", cancellationToken);
                var cuota = await context.CuotasMensuales.SingleOrDefaultAsync(c => c.EstudianteId == estudianteId && c.Anio == anio && c.Mes == mes);
                if (cuota is null)
                {
                    cuota = new CuotaMensual { EstudianteId = estudianteId, Anio = anio, Mes = mes, FechaVencimiento = new DateTime(anio, mes, 10, 23, 59, 59, DateTimeKind.Utc) };
                    context.CuotasMensuales.Add(cuota);
                }
                cuota.ComprobanteUrl = ruta;
                cuota.Estado = EstadoPago.EnRevision;
                cuota.Importe = importe ?? 0;
                cuota.MetodoPago = (metodoPago ?? string.Empty).Trim();
                cuota.ValidadoPorUsuarioId = null;
                cuota.FechaValidacionUtc = null;
                await context.SaveChangesAsync();
                return Results.Ok(new { mensaje = "El comprobante de la cuota quedó pendiente de revisión." });
            }
            catch (ArgumentException error) { return Results.BadRequest(new { mensaje = error.Message }); }
        }).DisableAntiforgery();

        personal.MapGet("/matricula/{anio:int}/comprobante", async (int anio, HttpContext http,
            AppDbContext context, DocumentoStorage storage) =>
        {
            var estudianteId = http.User.ObtenerUsuarioId();
            var ruta = await context.MatriculasIniciales.AsNoTracking().Where(m => m.EstudianteId == estudianteId && m.PeriodoLectivo == anio).Select(m => m.ComprobanteUrl).SingleOrDefaultAsync();
            return Descargar(storage, ruta, $"comprobante-matricula-{anio}");
        });
        personal.MapGet("/cuotas/{anio:int}/{mes:int}/comprobante", async (int anio, int mes,
            HttpContext http, AppDbContext context, DocumentoStorage storage) =>
        {
            var estudianteId = http.User.ObtenerUsuarioId();
            var ruta = await context.CuotasMensuales.AsNoTracking().Where(c => c.EstudianteId == estudianteId && c.Anio == anio && c.Mes == mes).Select(c => c.ComprobanteUrl).SingleOrDefaultAsync();
            return Descargar(storage, ruta, $"comprobante-cuota-{anio}-{mes:D2}");
        });
    }

    private static IResult Descargar(DocumentoStorage storage, string? ruta, string nombre)
    {
        var archivo = storage.Obtener(ruta);
        if (archivo is null) return Results.NotFound(new { mensaje = "No se encontró el comprobante." });
        var extension = Path.GetExtension(archivo.Value.Ruta);
        return Results.File(archivo.Value.Ruta, archivo.Value.ContentType, nombre + extension);
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
            "EN_REVISION" or "ENREVISION" => EstadoPago.EnRevision,
            "RECHAZADO" => EstadoPago.Rechazado,
            "PENDIENTE" or "" => EstadoPago.Pendiente,
            _ => throw new BadHttpRequestException("El estado de pago no es válido.")
        };

    private static string? ValidarDetalle(EstadoPagoActualizarDto dto)
    {
        if (dto.Importe < 0) return "El importe no puede ser negativo.";
        if ((dto.MetodoPago?.Length ?? 0) > 40) return "El método de pago no puede superar 40 caracteres.";
        if ((dto.Observaciones?.Length ?? 0) > 500) return "Las observaciones no pueden superar 500 caracteres.";
        if ((dto.ComprobanteUrl?.Length ?? 0) > 500) return "La URL del comprobante no puede superar 500 caracteres.";
        return null;
    }

    private static string? ValidarCarga(string? metodoPago, decimal? importe)
    {
        if (importe is < 0) return "El importe no puede ser negativo.";
        if ((metodoPago?.Length ?? 0) > 40) return "El método de pago no puede superar 40 caracteres.";
        return null;
    }
}
