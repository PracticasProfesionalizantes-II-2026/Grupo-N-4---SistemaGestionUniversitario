using AcadionApi.Datos;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class DirectivoEndpoints
{
    public static void MapDirectivoEndpoints(this IEndpointRouteBuilder routes)
    {
        var grupo = routes.MapGroup("/api/directivo")
            .RequireAuthorization(policy =>
            {
                policy.RequireRole(RolesSistema.Directivo, RolesSistema.Secretario);
                policy.RequireClaim(ClaimsSistema.Permiso, PermisosSistema.ReportesLeer);
            });

        grupo.MapGet("/panel", async (int? cicloLectivo, int? carreraId, AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            if (ciclo is < 2020 or > 2100)
                return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });

            if (carreraId.HasValue &&
                !await context.Set<Carrera>().AnyAsync(c => c.IdCarrera == carreraId.Value))
                return Results.BadRequest(new { mensaje = "La carrera seleccionada no existe." });

            var inscripciones = context.EstudianteMaterias.AsNoTracking()
                .Where(i => i.CicloLectivo == ciclo && i.Estado != "Cancelada" &&
                    i.Estudiante != null && i.Estudiante.Estado == EstadoUsuario.Activo &&
                    i.Materia != null && i.Materia.AnioCursada != null);
            var materias = context.Materias.AsNoTracking()
                .Where(m => m.Estado == "Activa" && m.AnioCursada != null);
            var examenesFinales = context.Set<Examen>().AsNoTracking()
                .Where(e => e.CicloLectivo == ciclo && e.TipoExamen == "Final" &&
                    e.Materia != null && e.Materia.AnioCursada != null);

            if (carreraId.HasValue)
            {
                var id = carreraId.Value;
                inscripciones = inscripciones.Where(i => i.Materia!.AnioCursada!.IdCarrera == id);
                materias = materias.Where(m => m.AnioCursada!.IdCarrera == id);
                examenesFinales = examenesFinales.Where(e => e.Materia!.AnioCursada!.IdCarrera == id);
            }

            var estudiantesActivos = context.Usuarios.AsNoTracking()
                .Where(u => u.RolId == RolesSistema.EstudianteId && u.Estado == EstadoUsuario.Activo);
            if (carreraId.HasValue)
                estudiantesActivos = estudiantesActivos.Where(u => u.CarreraId == carreraId.Value);

            var docentesActivos = carreraId.HasValue
                ? await context.DocentesMaterias.AsNoTracking()
                    .Where(dm => dm.CicloLectivo == ciclo && dm.Activa &&
                        dm.Docente != null && dm.Docente.Estado == EstadoUsuario.Activo &&
                        dm.Materia != null && dm.Materia.AnioCursada != null &&
                        dm.Materia.AnioCursada.IdCarrera == carreraId.Value)
                    .Select(dm => dm.IdDocente).Distinct().CountAsync()
                : await context.Usuarios.AsNoTracking().CountAsync(u =>
                    u.RolId == RolesSistema.DocenteId && u.Estado == EstadoUsuario.Activo);

            var carreras = await context.Set<Carrera>().AsNoTracking()
                .Where(c => c.Activa)
                .OrderBy(c => c.Nombre)
                .Select(c => new
                {
                    Id = c.IdCarrera,
                    c.Nombre,
                    Capacidad = c.CapacidadMaximaEstudiantes,
                    Estudiantes = c.AlumnosInscritos.Count(u =>
                        u.RolId == RolesSistema.EstudianteId && u.Estado == EstadoUsuario.Activo)
                })
                .ToListAsync();

            var alumnosPorCarrera = carreras
                .Where(c => !carreraId.HasValue || c.Id == carreraId.Value)
                .Select(c => new
                {
                    CarreraId = c.Id,
                    Carrera = c.Nombre,
                    c.Estudiantes,
                    c.Capacidad,
                    PorcentajeOcupacion = c.Capacidad <= 0
                        ? 0
                        : Math.Round(c.Estudiantes * 100m / c.Capacidad, 1)
                })
                .ToArray();

            var materiasConMasAlumnos = await inscripciones
                .GroupBy(i => new
                {
                    i.IdMateria,
                    i.Materia!.Nombre,
                    Carrera = i.Materia.AnioCursada!.Carrera!.Nombre
                })
                .Select(g => new
                {
                    MateriaId = g.Key.IdMateria,
                    Materia = g.Key.Nombre,
                    g.Key.Carrera,
                    Estudiantes = g.Select(i => i.IdEstudiante).Distinct().Count()
                })
                .OrderByDescending(x => x.Estudiantes)
                .ThenBy(x => x.Materia)
                .Take(8)
                .ToListAsync();

            var inasistencias = context.Set<Asistencia>().AsNoTracking()
                .Where(a => a.Inscripcion != null && a.Inscripcion.CicloLectivo == ciclo &&
                    a.Inscripcion.Estudiante != null &&
                    a.Inscripcion.Estudiante.Estado == EstadoUsuario.Activo &&
                    a.Inscripcion.Materia != null && a.Inscripcion.Materia.AnioCursada != null &&
                    (a.Tipo == "Ausente" || a.Tipo == "Justificada"));
            if (carreraId.HasValue)
                inasistencias = inasistencias.Where(a =>
                    a.Inscripcion!.Materia!.AnioCursada!.IdCarrera == carreraId.Value);

            var inasistenciasPorMateria = await inasistencias
                .GroupBy(a => new { a.Inscripcion!.IdMateria, a.Inscripcion.Materia!.Nombre })
                .Select(g => new
                {
                    MateriaId = g.Key.IdMateria,
                    Materia = g.Key.Nombre,
                    Inasistencias = g.Sum(a => a.CantidadInasistencias > 0 ? a.CantidadInasistencias : 1)
                })
                .OrderByDescending(x => x.Inasistencias)
                .ThenBy(x => x.Materia)
                .Take(8)
                .ToListAsync();

            var estadosCrudos = await inscripciones
                .GroupBy(i => i.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToListAsync();
            var totalCondiciones = estadosCrudos.Sum(e => e.Cantidad);
            int ContarCondicion(params string[] equivalentes) => estadosCrudos
                .Where(e => equivalentes.Contains(e.Estado, StringComparer.OrdinalIgnoreCase))
                .Sum(e => e.Cantidad);
            var condiciones = new[]
            {
                (Estado: "Cursando", Cantidad: ContarCondicion("Cursando", "En curso")),
                (Estado: "Regular", Cantidad: ContarCondicion("Regular", "Regularizada", "Regularizado")),
                (Estado: "Promocionado", Cantidad: ContarCondicion("Promocionada", "Promocionado")),
                (Estado: "Aprobado", Cantidad: ContarCondicion("Aprobada", "Aprobado")),
                (Estado: "Desaprobado", Cantidad: ContarCondicion("Desaprobada", "Desaprobado", "Libre"))
            }.Select(c => new
            {
                c.Estado,
                c.Cantidad,
                Porcentaje = totalCondiciones == 0
                    ? 0
                    : Math.Round(c.Cantidad * 100m / totalCondiciones, 1)
            }).ToArray();

            var hoyArgentina = ObtenerAhoraArgentina().Date;
            var examenesFinalesProximos = examenesFinales.Where(e => e.Fecha >= hoyArgentina);
            var cantidadFinalesProximos = await examenesFinalesProximos.CountAsync();
            var proximosFinales = await examenesFinalesProximos
                .OrderBy(e => e.Fecha)
                .Take(10)
                .Select(e => new
                {
                    ExamenId = e.IdExamen,
                    Materia = e.Materia!.Nombre,
                    e.Fecha,
                    Inscriptos = context.InscripcionesExamenes.Count(i =>
                        i.ExamenId == e.IdExamen && i.Estado == "Inscripto" &&
                        i.Estudiante.Estado == EstadoUsuario.Activo)
                })
                .ToListAsync();

            var idsEstudiantes = await estudiantesActivos.Select(u => u.Id).ToListAsync();
            var ahoraArgentina = ObtenerAhoraArgentina();
            var mesCorte = ciclo < ahoraArgentina.Year ? 12
                : ciclo == ahoraArgentina.Year ? ahoraArgentina.Month
                : 0;
            var cuotas = await context.CuotasMensuales.AsNoTracking()
                .Where(c => idsEstudiantes.Contains(c.EstudianteId) &&
                    c.Anio == ciclo && c.Mes <= mesCorte)
                .OrderBy(c => c.Mes)
                .ToListAsync();
            var cuotasPorEstudiante = cuotas
                .GroupBy(c => c.EstudianteId)
                .ToDictionary(g => g.Key, g => g.ToArray());
            var estadosPago = idsEstudiantes.Select(id =>
            {
                if (!cuotasPorEstudiante.TryGetValue(id, out var cuotasDelEstudiante) ||
                    cuotasDelEstudiante.Length == 0)
                    return EstadoPago.Pendiente;

                if (cuotasDelEstudiante[^1].Mes < mesCorte)
                    return EstadoPago.Pendiente;

                var tieneDeuda = cuotasDelEstudiante.Any(c =>
                    c.Estado is EstadoPago.Pendiente or EstadoPago.EnRevision or
                        EstadoPago.Vencida or EstadoPago.Impaga);
                if (tieneDeuda) return EstadoPago.Pendiente;

                return cuotasDelEstudiante[^1].Estado == EstadoPago.Exentado
                    ? EstadoPago.Exentado
                    : EstadoPago.AlDia;
            }).ToArray();
            var pagos = new
            {
                AlDia = estadosPago.Count(e => e == EstadoPago.AlDia),
                Pendientes = estadosPago.Count(e => e is EstadoPago.Pendiente or EstadoPago.EnRevision or EstadoPago.Vencida or EstadoPago.Impaga),
                Exentados = estadosPago.Count(e => e == EstadoPago.Exentado)
            };

            var desde = new DateOnly(ciclo, 1, 1);
            var hasta = new DateOnly(ciclo + 1, 1, 1);
            var asistenciaDocente = context.RegistrosAsistenciaPersonal.AsNoTracking()
                .Where(r => r.Usuario.RolId == RolesSistema.DocenteId &&
                    r.Usuario.Estado == EstadoUsuario.Activo &&
                    r.Fecha >= desde && r.Fecha < hasta);
            if (carreraId.HasValue)
                asistenciaDocente = asistenciaDocente.Where(r => r.Materia != null &&
                    r.Materia.AnioCursada != null &&
                    r.Materia.AnioCursada.IdCarrera == carreraId.Value);

            var asistenciaResumenCrudo = await asistenciaDocente
                .GroupBy(r => new { r.Estado, r.Justificada })
                .Select(g => new { g.Key.Estado, g.Key.Justificada, Cantidad = g.Count() })
                .ToListAsync();
            var asistenciaResumen = new
            {
                Presentes = asistenciaResumenCrudo.Where(x => x.Estado == "Presente").Sum(x => x.Cantidad),
                Ausentes = asistenciaResumenCrudo.Where(x => x.Estado == "Ausente" && !x.Justificada).Sum(x => x.Cantidad),
                Justificadas = asistenciaResumenCrudo.Where(x => x.Estado == "Ausente" && x.Justificada).Sum(x => x.Cantidad)
            };

            var docentesConAusencias = await asistenciaDocente
                .Where(r => r.Estado == "Ausente")
                .GroupBy(r => new
                {
                    r.UsuarioId,
                    r.Usuario.Persona.Apellido,
                    r.Usuario.Persona.Nombre
                })
                .Select(g => new
                {
                    DocenteId = g.Key.UsuarioId,
                    Docente = g.Key.Apellido + ", " + g.Key.Nombre,
                    Ausencias = g.Count(),
                    Justificadas = g.Count(r => r.Justificada)
                })
                .OrderByDescending(x => x.Ausencias)
                .ThenBy(x => x.Docente)
                .Take(10)
                .ToListAsync();

            var resumen = new
            {
                EstudiantesActivos = await estudiantesActivos.CountAsync(),
                DocentesActivos = docentesActivos,
                CarrerasVigentes = carreras.Count,
                MateriasActivas = await materias.CountAsync(),
                FinalesProximos = cantidadFinalesProximos
            };

            return Results.Ok(new
            {
                CicloLectivo = ciclo,
                CarreraId = carreraId,
                Carreras = carreras.Select(c => new { c.Id, c.Nombre }),
                Resumen = resumen,
                AlumnosPorCarrera = alumnosPorCarrera,
                MateriasConMasAlumnos = materiasConMasAlumnos,
                InasistenciasPorMateria = inasistenciasPorMateria,
                CondicionesAcademicas = condiciones,
                ProximosFinales = proximosFinales,
                Pagos = pagos,
                AsistenciaDocente = asistenciaResumen,
                DocentesConAusencias = docentesConAusencias
            });
        });

        grupo.MapGet("/detalle/carreras/{carreraId:int}", async (int carreraId,
            int? cicloLectivo, AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            var carrera = await context.Set<Carrera>().AsNoTracking()
                .Where(c => c.IdCarrera == carreraId)
                .Select(c => c.Nombre).SingleOrDefaultAsync();
            if (carrera is null) return Results.NotFound(new { mensaje = "La carrera no existe." });
            var estudiantes = await context.Usuarios.AsNoTracking()
                .Where(u => u.RolId == RolesSistema.EstudianteId && u.CarreraId == carreraId &&
                    u.Estado == EstadoUsuario.Activo)
                .OrderBy(u => u.Persona.Apellido).ThenBy(u => u.Persona.Nombre)
                .Select(u => new
                {
                    Nombre = u.Persona.Apellido + ", " + u.Persona.Nombre,
                    u.Legajo,
                    Materias = context.EstudianteMaterias.Count(i => i.IdEstudiante == u.Id &&
                        i.CicloLectivo == ciclo && i.Estado != "Cancelada")
                }).ToListAsync();
            return Results.Ok(new { Titulo = carrera, Subtitulo = $"Estudiantes activos · ciclo {ciclo}", Estudiantes = estudiantes });
        });

        grupo.MapGet("/detalle/materias/{materiaId:int}/inasistencias", async (int materiaId,
            int? cicloLectivo, AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            var materia = await context.Materias.AsNoTracking().Where(m => m.IdMateria == materiaId)
                .Select(m => new { m.Nombre, Carrera = m.AnioCursada!.Carrera!.Nombre }).SingleOrDefaultAsync();
            if (materia is null) return Results.NotFound(new { mensaje = "La materia no existe." });
            var estudiantes = await context.Set<Asistencia>().AsNoTracking()
                .Where(a => a.Inscripcion != null && a.Inscripcion.IdMateria == materiaId &&
                    a.Inscripcion.Estudiante != null &&
                    a.Inscripcion.Estudiante.Estado == EstadoUsuario.Activo &&
                    a.Inscripcion.CicloLectivo == ciclo && (a.Tipo == "Ausente" || a.Tipo == "Justificada"))
                .GroupBy(a => new { a.Inscripcion!.IdEstudiante, a.Inscripcion.Estudiante!.Persona.Apellido, a.Inscripcion.Estudiante.Persona.Nombre, a.Inscripcion.Estudiante.Legajo })
                .Select(g => new
                {
                    Nombre = g.Key.Apellido + ", " + g.Key.Nombre,
                    g.Key.Legajo,
                    Inasistencias = g.Sum(a => a.CantidadInasistencias > 0 ? a.CantidadInasistencias : 1),
                    Justificadas = g.Count(a => a.Justificada || a.Tipo == "Justificada")
                }).OrderByDescending(x => x.Inasistencias).ThenBy(x => x.Nombre).ToListAsync();
            return Results.Ok(new { Titulo = materia.Nombre, Subtitulo = $"{materia.Carrera} · ciclo {ciclo}", Estudiantes = estudiantes });
        });
    }

    private static DateTime ObtenerAhoraArgentina()
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time"));
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"));
        }
    }

}
