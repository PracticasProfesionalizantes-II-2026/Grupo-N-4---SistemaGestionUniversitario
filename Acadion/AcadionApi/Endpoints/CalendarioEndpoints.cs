using AcadionApi.Datos;
using AcadionApi.Logica;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class CalendarioEndpoints
{
    private static readonly string[] Tipos =
        ["Inicio de clases", "Inicio de cuatrimestre", "Fin de cuatrimestre", "Feriado",
            "Día sin clases", "Inscripción", "Mesa final", "Vencimiento", "Institucional"];

    public static void MapCalendarioEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/calendario").RequireAuthorization();

        group.MapGet("/", async (int? cicloLectivo, int? carreraId, AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            if (ciclo is < 2020 or > 2100)
                return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });
            var desde = new DateTime(ciclo, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var hasta = desde.AddYears(1);

            var eventos = await context.Set<EventoCalendario>().AsNoTracking()
                .Where(e => e.Activo && e.FechaInicioUtc < hasta && e.FechaFinUtc >= desde &&
                    (!carreraId.HasValue || e.CarreraId == null || e.CarreraId == carreraId))
                .Include(e => e.Carrera)
                .Select(e => new CalendarioItem(e.Id, e.Titulo, e.Tipo, e.Descripcion,
                    e.FechaInicioUtc, e.FechaFinUtc, e.CarreraId,
                    e.Carrera == null ? "Todas las carreras" : e.Carrera.Nombre, false))
                .ToListAsync();

            var periodosMaterias = await context.PeriodosInscripcionMaterias.AsNoTracking()
                .Where(p => p.CicloLectivo == ciclo && p.MateriaId == null &&
                    (!carreraId.HasValue || p.CarreraId == carreraId))
                .Include(p => p.Carrera)
                .Select(p => new CalendarioItem(-p.Id,
                    "Inscripción a materias", "Inscripción",
                    "Período general de inscripción a materias.", p.FechaInicioUtc, p.FechaFinUtc,
                    p.CarreraId, p.Carrera.Nombre, true))
                .ToListAsync();

            var periodoFinales = await context.PeriodosInscripcionExamenes.AsNoTracking()
                .Where(p => p.CicloLectivo == ciclo)
                .Select(p => new CalendarioItem(-100000 - p.Id,
                    "Inscripción a exámenes finales", "Inscripción",
                    "Período institucional de inscripción a finales.", p.FechaInicioUtc, p.FechaFinUtc,
                    null, "Todas las carreras", true))
                .ToListAsync();

            var finales = await context.Set<Examen>().AsNoTracking()
                .Where(e => e.CicloLectivo == ciclo && e.TipoExamen == "Final" &&
                    (!carreraId.HasValue || e.Materia!.AnioCursada!.IdCarrera == carreraId))
                .Select(e => new CalendarioItem(-200000 - e.IdExamen,
                    "Final de " + e.Materia!.Nombre, "Mesa final",
                    e.TurnoExamenFinal == null
                        ? "Examen final programado."
                        : e.TurnoExamenFinal.Nombre + " · " + e.TurnoExamenFinal.NumeroLlamado + ".º llamado.",
                    e.Fecha, e.Fecha.AddHours(2),
                    e.Materia.AnioCursada!.IdCarrera, e.Materia.AnioCursada.Carrera!.Nombre, true))
                .ToListAsync();

            return Results.Ok(eventos.Concat(periodosMaterias).Concat(periodoFinales).Concat(finales)
                .OrderBy(e => e.FechaInicioUtc));
        });

        group.MapGet("/personal", async (int? cicloLectivo, HttpContext http,
            AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            if (ciclo is < 2020 or > 2100)
                return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });

            var usuarioId = http.User.ObtenerUsuarioId();
            var usuario = await context.Usuarios.AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => new { u.RolId, u.CarreraId })
                .SingleOrDefaultAsync();
            if (usuario is null) return Results.NotFound();
            if (usuario.RolId is not (RolesSistema.EstudianteId or RolesSistema.DocenteId))
                return Results.Forbid();

            var carreras = new HashSet<int>();
            var materias = new HashSet<int>();
            if (usuario.RolId == RolesSistema.EstudianteId)
            {
                if (usuario.CarreraId.HasValue) carreras.Add(usuario.CarreraId.Value);
                var materiasEstudiante = await context.EstudianteMaterias.AsNoTracking()
                    .Where(i => i.IdEstudiante == usuarioId && i.CicloLectivo == ciclo &&
                        i.Estado != "Cancelada")
                    .Select(i => i.IdMateria)
                    .Distinct()
                    .ToListAsync();
                materias.UnionWith(materiasEstudiante);
            }
            else
            {
                var asignaciones = await context.DocentesMaterias.AsNoTracking()
                    .Where(dm => dm.IdDocente == usuarioId && dm.CicloLectivo == ciclo && dm.Activa)
                    .Select(dm => new
                    {
                        MateriaId = dm.IdMateria,
                        CarreraId = dm.Materia.AnioCursada!.IdCarrera
                    })
                    .ToListAsync();
                materias.UnionWith(asignaciones.Select(a => a.MateriaId));
                carreras.UnionWith(asignaciones.Select(a => a.CarreraId));
            }

            var desde = new DateTime(ciclo, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var hasta = desde.AddYears(1);
            var idsCarrera = carreras.ToArray();
            var idsMateria = materias.ToArray();

            var eventos = await context.EventosCalendario.AsNoTracking()
                .Where(e => e.Activo && e.FechaInicioUtc < hasta && e.FechaFinUtc >= desde &&
                    (e.CarreraId == null || idsCarrera.Contains(e.CarreraId.Value)))
                .Include(e => e.Carrera)
                .Select(e => new CalendarioItem(e.Id, e.Titulo, e.Tipo, e.Descripcion,
                    e.FechaInicioUtc, e.FechaFinUtc, e.CarreraId,
                    e.Carrera == null ? "Todas las carreras" : e.Carrera.Nombre, false))
                .ToListAsync();

            var periodosMaterias = await context.PeriodosInscripcionMaterias.AsNoTracking()
                .Where(p => p.CicloLectivo == ciclo && p.MateriaId == null &&
                    idsCarrera.Contains(p.CarreraId))
                .Include(p => p.Carrera)
                .Select(p => new CalendarioItem(-p.Id, "Inscripción a materias", "Inscripción",
                    "Período general de inscripción a materias.", p.FechaInicioUtc, p.FechaFinUtc,
                    p.CarreraId, p.Carrera.Nombre, true))
                .ToListAsync();

            var periodoFinales = await context.PeriodosInscripcionExamenes.AsNoTracking()
                .Where(p => p.CicloLectivo == ciclo)
                .Select(p => new CalendarioItem(-100000 - p.Id,
                    "Inscripción a exámenes finales", "Inscripción",
                    "Período institucional de inscripción a finales.", p.FechaInicioUtc,
                    p.FechaFinUtc, null, "Todas las carreras", true))
                .ToListAsync();

            var consultaExamenes = context.Set<Examen>().AsNoTracking()
                .Where(e => e.CicloLectivo == ciclo);
            consultaExamenes = usuario.RolId == RolesSistema.DocenteId
                ? consultaExamenes.Where(e => e.IdDocente == usuarioId)
                : consultaExamenes.Where(e => idsMateria.Contains(e.IdMateria));
            var examenesBase = await consultaExamenes
                .Select(e => new
                {
                    e.IdExamen,
                    e.IdMateria,
                    Materia = e.Materia!.Nombre,
                    CarreraId = e.Materia.AnioCursada!.IdCarrera,
                    Carrera = e.Materia.AnioCursada.Carrera!.Nombre,
                    e.TipoExamen,
                    e.Fecha
                })
                .ToListAsync();
            var examenes = examenesBase.Select(e => new CalendarioItem(
                -200000 - e.IdExamen,
                $"{FormatearTipoExamen(e.TipoExamen)} de {e.Materia}",
                FormatearTipoExamen(e.TipoExamen),
                "Evaluación programada para la materia.", e.Fecha, e.Fecha.AddHours(2),
                e.CarreraId, e.Carrera, true));

            return Results.Ok(eventos.Concat(periodosMaterias).Concat(periodoFinales)
                .Concat(examenes).OrderBy(e => e.FechaInicioUtc));
        });

        group.MapPost("/", async (EventoCalendarioDto dto, HttpContext http, AppDbContext context) =>
        {
            var validation = await ValidarAsync(dto, context);
            if (validation is not null) return validation;
            var evento = new EventoCalendario
            {
                Titulo = dto.Titulo.Trim(), Tipo = NormalizarTipo(dto.Tipo),
                Descripcion = dto.Descripcion.Trim(), FechaInicioUtc = dto.FechaInicio.ToUniversalTime(),
                FechaFinUtc = dto.FechaFin.ToUniversalTime(), CarreraId = dto.CarreraId,
                CreadoPorUsuarioId = http.User.ObtenerUsuarioId()
            };
            context.Add(evento);
            await context.SaveChangesAsync();
            return Results.Created($"/api/calendario/{evento.Id}", new { evento.Id });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);

        group.MapPut("/{id:int}", async (int id, EventoCalendarioDto dto,
            AppDbContext context) =>
        {
            var validation = await ValidarAsync(dto, context);
            if (validation is not null) return validation;
            var evento = await context.Set<EventoCalendario>().SingleOrDefaultAsync(e => e.Id == id);
            if (evento is null) return Results.NotFound();
            evento.Titulo = dto.Titulo.Trim(); evento.Tipo = NormalizarTipo(dto.Tipo);
            evento.Descripcion = dto.Descripcion.Trim();
            evento.FechaInicioUtc = dto.FechaInicio.ToUniversalTime();
            evento.FechaFinUtc = dto.FechaFin.ToUniversalTime();
            evento.CarreraId = dto.CarreraId; evento.Activo = dto.Activo;
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Evento actualizado correctamente." });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);

        group.MapDelete("/{id:int}", async (int id, AppDbContext context) =>
        {
            var evento = await context.Set<EventoCalendario>().SingleOrDefaultAsync(e => e.Id == id);
            if (evento is null) return Results.NotFound();
            evento.Activo = false;
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Evento eliminado del calendario." });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);
    }

    private static async Task<IResult?> ValidarAsync(EventoCalendarioDto dto, AppDbContext context)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo) || dto.Titulo.Trim().Length > 150)
            return Results.BadRequest(new { mensaje = "El título es obligatorio y admite hasta 150 caracteres." });
        try { NormalizadorDatos.ValidarTextoAcademico(dto.Titulo, "El título"); }
        catch (ArgumentException ex) { return Results.BadRequest(new { mensaje = ex.Message }); }
        if (!Tipos.Contains(dto.Tipo, StringComparer.OrdinalIgnoreCase))
            return Results.BadRequest(new { mensaje = "El tipo de evento no es válido." });
        if (dto.FechaFin <= dto.FechaInicio)
            return Results.BadRequest(new { mensaje = "La fecha final debe ser posterior al inicio." });
        if (dto.CarreraId.HasValue && !await context.Set<Carrera>().AnyAsync(c => c.IdCarrera == dto.CarreraId))
            return Results.BadRequest(new { mensaje = "La carrera indicada no existe." });
        return null;
    }

    private static string NormalizarTipo(string tipo) => Tipos.Single(t =>
        t.Equals(tipo.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string FormatearTipoExamen(string tipo) => tipo switch
    {
        "TrabajoPractico" => "Trabajo práctico",
        "Presentacion" => "Presentación",
        "Final" => "Final",
        "Recuperatorio" => "Recuperatorio",
        _ => "Parcial"
    };

    private sealed record CalendarioItem(int Id, string Titulo, string Tipo, string Descripcion,
        DateTime FechaInicioUtc, DateTime FechaFinUtc, int? CarreraId, string Carrera, bool Automatico);
}

public class EventoCalendarioDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Institucional";
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int? CarreraId { get; set; }
    public bool Activo { get; set; } = true;
}
