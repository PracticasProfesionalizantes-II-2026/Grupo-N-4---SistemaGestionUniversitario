using AcadionApi.Datos;
using AcadionApi.Logica;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class TurnosExamenFinalEndpoints
{
    public static void MapTurnosExamenFinalEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/turnos-final").RequireAuthorization();

        group.MapGet("/", async (int? cicloLectivo, bool? incluirInactivos, AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            var consulta = context.TurnosExamenFinal.AsNoTracking()
                .Where(t => t.CicloLectivo == ciclo);
            if (incluirInactivos != true) consulta = consulta.Where(t => t.Activo);
            return Results.Ok(await consulta
                .OrderBy(t => t.FechaInicioUtc).ThenBy(t => t.NumeroLlamado)
                .Select(t => new
                {
                    t.Id, t.CicloLectivo, t.Nombre, t.NumeroLlamado,
                    t.FechaInicioUtc, t.FechaFinUtc, t.Activo,
                    CantidadExamenes = t.Examenes.Count
                }).ToListAsync());
        });

        group.MapPost("/", async (TurnoExamenFinalDto dto, AppDbContext context) =>
        {
            var error = Validar(dto);
            if (error is not null) return error;
            if (await ExisteAsync(dto, null, context))
                return Results.Conflict(new { mensaje = "Ya existe ese llamado para el turno y ciclo lectivo." });
            var turno = new TurnoExamenFinal
            {
                CicloLectivo = dto.CicloLectivo,
                Nombre = dto.Nombre.Trim(),
                NumeroLlamado = dto.NumeroLlamado,
                FechaInicioUtc = dto.FechaInicio.ToUniversalTime(),
                FechaFinUtc = dto.FechaFin.ToUniversalTime(),
                Activo = dto.Activo
            };
            context.Add(turno);
            await context.SaveChangesAsync();
            return Results.Created($"/api/turnos-final/{turno.Id}", new { turno.Id });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);

        group.MapPut("/{id:int}", async (int id, TurnoExamenFinalDto dto, AppDbContext context) =>
        {
            var error = Validar(dto);
            if (error is not null) return error;
            var turno = await context.TurnosExamenFinal.SingleOrDefaultAsync(t => t.Id == id);
            if (turno is null) return Results.NotFound(new { mensaje = "El turno no existe." });
            if (await ExisteAsync(dto, id, context))
                return Results.Conflict(new { mensaje = "Ya existe ese llamado para el turno y ciclo lectivo." });
            var examenesFuera = await context.Set<Examen>().AnyAsync(e =>
                e.TurnoExamenFinalId == id && (e.Fecha < dto.FechaInicio || e.Fecha > dto.FechaFin));
            if (examenesFuera)
                return Results.Conflict(new { mensaje = "No se puede reducir el rango porque dejaría finales programados fuera del turno." });
            turno.CicloLectivo = dto.CicloLectivo;
            turno.Nombre = dto.Nombre.Trim();
            turno.NumeroLlamado = dto.NumeroLlamado;
            turno.FechaInicioUtc = dto.FechaInicio.ToUniversalTime();
            turno.FechaFinUtc = dto.FechaFin.ToUniversalTime();
            turno.Activo = dto.Activo;
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Turno actualizado correctamente." });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);

        group.MapDelete("/{id:int}", async (int id, AppDbContext context) =>
        {
            var turno = await context.TurnosExamenFinal.SingleOrDefaultAsync(t => t.Id == id);
            if (turno is null) return Results.NotFound(new { mensaje = "El turno no existe." });
            turno.Activo = false;
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "El turno quedó inactivo; los finales existentes conservaron su historial." });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);
    }

    private static IResult? Validar(TurnoExamenFinalDto dto)
    {
        if (dto.CicloLectivo is < 2020 or > 2100)
            return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });
        if (dto.NumeroLlamado is < 1 or > 5)
            return Results.BadRequest(new { mensaje = "El número de llamado debe estar entre 1 y 5." });
        if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Nombre.Trim().Length > 80)
            return Results.BadRequest(new { mensaje = "Ingresá un nombre de turno válido." });
        try { NormalizadorDatos.ValidarTextoAcademico(dto.Nombre, "El nombre del turno"); }
        catch (ArgumentException ex) { return Results.BadRequest(new { mensaje = ex.Message }); }
        if (dto.FechaFin <= dto.FechaInicio)
            return Results.BadRequest(new { mensaje = "El final del turno debe ser posterior a su inicio." });
        if (dto.FechaInicio.Year != dto.CicloLectivo || dto.FechaFin.Year != dto.CicloLectivo)
            return Results.BadRequest(new { mensaje = "Las fechas del turno deben pertenecer al ciclo lectivo seleccionado." });
        return null;
    }

    private static Task<bool> ExisteAsync(TurnoExamenFinalDto dto, int? excluirId, AppDbContext context) =>
        context.TurnosExamenFinal.AnyAsync(t => t.Id != excluirId &&
            t.CicloLectivo == dto.CicloLectivo && t.NumeroLlamado == dto.NumeroLlamado &&
            t.Nombre == dto.Nombre.Trim());
}

public class TurnoExamenFinalDto
{
    public int CicloLectivo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int NumeroLlamado { get; set; } = 1;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool Activo { get; set; } = true;
}
