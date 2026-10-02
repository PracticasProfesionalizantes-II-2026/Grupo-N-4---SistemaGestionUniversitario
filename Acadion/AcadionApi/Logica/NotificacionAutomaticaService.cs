using AcadionApi.Datos;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Logica;

public static class NotificacionAutomaticaService
{
    public static async Task ReprogramarPendientesAsync(AppDbContext context)
    {
        var ahora = DateTime.UtcNow;
        var periodosExamen = await context.PeriodosInscripcionExamenes.AsNoTracking()
            .Where(p => p.FechaFinUtc > ahora)
            .ToListAsync();
        foreach (var periodo in periodosExamen)
            await ReprogramarPeriodoExamenesAsync(periodo, periodo.ModificadoPorUsuarioId, context);

        var periodosMateria = await context.PeriodosInscripcionMaterias.AsNoTracking()
            .Where(p => p.FechaFinUtc > ahora)
            .ToListAsync();
        foreach (var periodo in periodosMateria
            .GroupBy(p => new { p.CarreraId, p.CicloLectivo })
            .Select(g => g.OrderByDescending(p => p.FechaModificacionUtc).First()))
            await ReprogramarPeriodoMateriaAsync(periodo, periodo.ModificadoPorUsuarioId, context);

        var hoyArgentina = TimeZoneInfo.ConvertTimeFromUtc(ahora, ZonaArgentina()).Date;
        var examenesFuturos = await context.Set<Examen>().AsNoTracking()
            .Where(e => e.Fecha > hoyArgentina)
            .Select(e => e.IdExamen)
            .ToListAsync();
        foreach (var examenId in examenesFuturos)
            await ReprogramarExamenAsync(examenId, context);

        await context.SaveChangesAsync();
    }

    public static async Task ReprogramarPeriodoExamenesAsync(
        PeriodoInscripcionExamen periodo, int? creadorId, AppDbContext context)
    {
        var prefijo = $"AUTO:PERIODO_EXAMEN:{periodo.CicloLectivo}:";
        await EliminarPorPrefijoAsync(prefijo, context);
        Agregar(context, $"{prefijo}APERTURA", "Inscripción a exámenes habilitada",
            $"Ya se encuentra abierta la inscripción a exámenes finales del ciclo {periodo.CicloLectivo}.",
            periodo.FechaInicioUtc, periodo.FechaFinUtc, creadorId, RolesSistema.EstudianteId, null, "Alta");
        Agregar(context, $"{prefijo}CIERRE", "La inscripción a exámenes finaliza en 24 horas",
            "Te queda un día para completar tu inscripción a los exámenes finales.",
            periodo.FechaFinUtc.AddHours(-24), periodo.FechaFinUtc, creadorId,
            RolesSistema.EstudianteId, null, "Alta");
    }

    public static async Task ReprogramarPeriodoMateriaAsync(
        PeriodoInscripcionMateria periodo, int? creadorId, AppDbContext context)
    {
        await EliminarPorPrefijoAsync("AUTO:PERIODO_MATERIA:", context);
        var prefijo = $"AUTO:PERIODO_MATERIAS:CARRERA:{periodo.CarreraId}:CICLO:{periodo.CicloLectivo}:";
        await EliminarPorPrefijoAsync(prefijo, context);
        var periodos = await context.PeriodosInscripcionMaterias.AsNoTracking()
            .Where(p => p.CarreraId == periodo.CarreraId && p.CicloLectivo == periodo.CicloLectivo)
            .ToListAsync();
        var inicio = periodos.Min(p => p.FechaInicioUtc);
        var fin = periodos.Max(p => p.FechaFinUtc);
        var estudiantes = await context.Usuarios.AsNoTracking()
            .Where(u => u.RolId == RolesSistema.EstudianteId && u.CarreraId == periodo.CarreraId)
            .Select(u => u.Id)
            .ToListAsync();
        foreach (var estudianteId in estudiantes)
        {
            Agregar(context, $"{prefijo}ESTUDIANTE:{estudianteId}:APERTURA",
                "Inscripción a materias habilitada",
                $"Ya se encuentra abierta la inscripción a materias del ciclo {periodo.CicloLectivo}.",
                inicio, fin, creadorId, null, estudianteId, "Alta");
            Agregar(context, $"{prefijo}ESTUDIANTE:{estudianteId}:CIERRE",
                "La inscripción a materias finaliza en 24 horas",
                "Te queda un día para completar tu inscripción a materias.", fin.AddHours(-24),
                fin, creadorId, null, estudianteId, "Alta");
        }
    }

    public static async Task NotificarCambioFechaFinalAsync(int examenId, DateTime fechaAnterior,
        AppDbContext context)
    {
        var examen = await context.Set<Examen>().AsNoTracking()
            .Include(e => e.Materia)
            .SingleAsync(e => e.IdExamen == examenId);
        if (!examen.TipoExamen.Equals("Final", StringComparison.OrdinalIgnoreCase) ||
            examen.Fecha == fechaAnterior)
            return;

        var ahora = DateTime.UtcNow;
        var expiracion = examen.Fecha.Date >= DateTime.Today
            ? FinDelDiaUtc(examen.Fecha)
            : ahora.AddDays(30);
        var estudiantes = await context.InscripcionesExamenes.AsNoTracking()
            .Where(i => i.ExamenId == examenId && i.Estado == "Inscripto")
            .Select(i => i.EstudianteId)
            .Distinct()
            .ToListAsync();
        foreach (var estudianteId in estudiantes)
        {
            Agregar(context,
                $"AUTO:CAMBIO_FECHA_FINAL:{examenId}:{ahora.Ticks}:ESTUDIANTE:{estudianteId}",
                "Se modificó la fecha de un examen final",
                $"El final de {examen.Materia?.Nombre ?? "tu materia"} cambió del " +
                $"{fechaAnterior:dd/MM/yyyy HH:mm} al {examen.Fecha:dd/MM/yyyy HH:mm}.",
                ahora, expiracion, examen.IdDocente, null, estudianteId, "Alta");
        }
    }

    public static async Task ReprogramarExamenAsync(int examenId, AppDbContext context)
    {
        var examen = await context.Set<Examen>().AsNoTracking()
            .Include(e => e.Materia)
            .SingleAsync(e => e.IdExamen == examenId);
        var prefijo = $"AUTO:EXAMEN:{examenId}:";
        await EliminarPorPrefijoAsync(prefijo, context);
        var estudiantes = examen.TipoExamen.Equals("Final", StringComparison.OrdinalIgnoreCase)
            ? await context.InscripcionesExamenes.AsNoTracking()
                .Where(i => i.ExamenId == examenId && i.Estado == "Inscripto")
                .Select(i => i.EstudianteId).Distinct().ToListAsync()
            : await context.EstudianteMaterias.AsNoTracking()
                .Where(i => i.IdMateria == examen.IdMateria && i.CicloLectivo == examen.CicloLectivo &&
                    i.IdDocente == examen.IdDocente && i.Estado != "Cancelada")
                .Select(i => i.IdEstudiante).Distinct().ToListAsync();
        foreach (var estudianteId in estudiantes)
            ProgramarRecordatorioExamen(examen, estudianteId, context);
    }

    public static void ProgramarRecordatorioExamen(Examen examen, int estudianteId,
        AppDbContext context)
    {
        var publicacion = MedianocheAnteriorUtc(examen.Fecha);
        var expiracion = FinDelDiaUtc(examen.Fecha);
        var tipo = FormatearTipo(examen.TipoExamen);
        Agregar(context, $"AUTO:EXAMEN:{examen.IdExamen}:ESTUDIANTE:{estudianteId}",
            $"Mañana tenés {tipo}",
            $"Recordatorio: mañana rendís {tipo} de {examen.Materia?.Nombre ?? "tu materia"}.",
            publicacion, expiracion, examen.IdDocente, null, estudianteId, "Alta");
    }

    public static Task EliminarRecordatoriosExamenAsync(int examenId, AppDbContext context) =>
        EliminarPorPrefijoAsync($"AUTO:EXAMEN:{examenId}:", context);

    public static async Task NotificarNotaCargadaAsync(Examen examen, int estudianteId,
        bool esActualizacion, AppDbContext context)
    {
        var clave = $"AUTO:NOTA:{examen.IdExamen}:ESTUDIANTE:{estudianteId}";
        var tipo = FormatearTipo(examen.TipoExamen);
        var materia = examen.Materia?.Nombre ?? "tu materia";
        var titulo = esActualizacion ? "Se actualizó una nota" : "Se cargó una nueva nota";
        var mensaje = esActualizacion
            ? $"El docente actualizó tu nota de {tipo} de {materia}. Consultala en Reportes."
            : $"El docente cargó tu nota de {tipo} de {materia}. Consultala en Reportes.";
        var ahora = DateTime.UtcNow;
        var existente = await context.NotificacionesGenerales
            .SingleOrDefaultAsync(n => n.ClaveAutomatica == clave);
        if (existente is null)
        {
            Agregar(context, clave, titulo, mensaje, ahora, null, examen.IdDocente,
                null, estudianteId, "Alta");
            return;
        }

        existente.Titulo = titulo;
        existente.Mensaje = mensaje;
        existente.Prioridad = "Alta";
        existente.FechaPublicacionUtc = ahora;
        existente.FechaExpiracionUtc = null;
        existente.CreadaPorUsuarioId = examen.IdDocente;
        existente.Activa = true;
        var lecturas = await context.NotificacionesLecturas
            .Where(l => l.NotificacionId == existente.Id && l.UsuarioId == estudianteId)
            .ToListAsync();
        context.NotificacionesLecturas.RemoveRange(lecturas);
    }

    private static void Agregar(AppDbContext context, string clave, string titulo, string mensaje,
        DateTime publicacionUtc, DateTime? expiracionUtc, int? creadorId, int? rolId,
        int? usuarioId, string prioridad)
    {
        if (expiracionUtc.HasValue && expiracionUtc.Value <= DateTime.UtcNow) return;
        context.NotificacionesGenerales.Add(new NotificacionGeneral
        {
            ClaveAutomatica = clave,
            Titulo = titulo,
            Mensaje = mensaje,
            RolDestinoId = rolId,
            UsuarioDestinoId = usuarioId,
            Prioridad = prioridad,
            FechaPublicacionUtc = publicacionUtc,
            FechaExpiracionUtc = expiracionUtc,
            CreadaPorUsuarioId = creadorId,
            Activa = true
        });
    }

    private static async Task EliminarPorPrefijoAsync(string prefijo, AppDbContext context)
    {
        var existentes = await context.NotificacionesGenerales
            .Where(n => n.ClaveAutomatica != null && n.ClaveAutomatica.StartsWith(prefijo))
            .ToListAsync();
        context.NotificacionesGenerales.RemoveRange(existentes);
    }

    private static DateTime MedianocheAnteriorUtc(DateTime fecha)
    {
        var local = DateTime.SpecifyKind(fecha.Date.AddDays(-1), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, ZonaArgentina());
    }

    private static DateTime FinDelDiaUtc(DateTime fecha)
    {
        var local = DateTime.SpecifyKind(fecha.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, ZonaArgentina());
    }

    private static TimeZoneInfo ZonaArgentina()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"); }
    }

    private static string FormatearTipo(string tipo) => tipo switch
    {
        "TrabajoPractico" => "un trabajo práctico",
        "Presentacion" => "una presentación",
        "Final" => "un examen final",
        "Parcial" => "un parcial",
        "Recuperatorio" => "un recuperatorio",
        _ => "una evaluación"
    };
}
