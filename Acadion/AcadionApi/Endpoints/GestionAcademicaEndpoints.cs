using AcadionApi.Datos;
using AcadionApi.DTOs;
using AcadionApi.Logica;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class GestionAcademicaEndpoints
{
    public static void MapGestionAcademicaEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/gestion-academica")
            .RequierePermiso(PermisosSistema.AcademicoGestionar);

        group.MapPost("/anios-con-materias", async (AnioConMateriasCrearDto dto,
            AppDbContext context) =>
        {
            if (!await context.Set<Carrera>().AnyAsync(c => c.IdCarrera == dto.CarreraId))
                return Results.BadRequest(new { mensaje = "La carrera indicada no existe." });
            if (dto.NumeroAnio <= 0 || string.IsNullOrWhiteSpace(dto.NombreAnio))
                return Results.BadRequest(new { mensaje = "El año académico no es válido." });
            if (dto.Materias.Count == 0 || dto.Materias.Any(m => string.IsNullOrWhiteSpace(m.Nombre)))
                return Results.BadRequest(new { mensaje = "Debe indicar al menos una materia válida." });
            try
            {
                foreach (var materia in dto.Materias)
                    NormalizadorDatos.ValidarTextoAcademico(materia.Nombre, "El nombre de la materia");
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { mensaje = ex.Message });
            }
            if (dto.Materias.SelectMany(m => m.Horarios).Any(h =>
                h.HoraInicio >= h.HoraFin || h.HoraFin - h.HoraInicio < TimeSpan.FromMinutes(40)))
                return Results.BadRequest(new
                {
                    mensaje = "Todos los horarios deben tener un rango válido y una duración mínima de 40 minutos."
                });
            if (await context.Set<Anio>().AnyAsync(a =>
                a.IdCarrera == dto.CarreraId && a.NumeroAnio == dto.NumeroAnio))
                return Results.Conflict(new { mensaje = "Ese año ya existe dentro de la carrera." });

            await using var transaccion = await context.Database.BeginTransactionAsync();
            var anio = new Anio
            {
                IdCarrera = dto.CarreraId,
                NumeroAnio = dto.NumeroAnio,
                NombreAnio = dto.NombreAnio.Trim(),
                Materias = dto.Materias.Select(m => new Materia
                {
                    Nombre = m.Nombre.Trim(),
                    Modalidad = m.Modalidad.Trim(),
                    Estado = "Activa",
                    Horarios = m.Horarios.Select(h => new HorarioMateria
                    {
                        DiaSemana = h.DiaSemana.Trim(),
                        HoraInicio = h.HoraInicio,
                        HoraFin = h.HoraFin
                    }).ToList()
                }).ToList()
            };

            context.Add(anio);
            await context.SaveChangesAsync();
            await transaccion.CommitAsync();
            return Results.Created($"/anios/{anio.IdAnio}", new
            {
                anio.IdAnio,
                anio.NumeroAnio,
                anio.NombreAnio,
                Materias = anio.Materias.Select(m => new { m.IdMateria, m.Nombre })
            });
        });

        group.MapPost("/docentes-materias", async (DocenteMateriaCrearDto dto,
            AppDbContext context) =>
        {
            var docente = await context.Usuarios.SingleOrDefaultAsync(u => u.Id == dto.DocenteId);
            if (docente is null || docente.RolId != RolesSistema.DocenteId)
                return Results.BadRequest(new { mensaje = "El usuario indicado no es un docente." });
            var materia = await context.Materias
                .SingleOrDefaultAsync(m => m.IdMateria == dto.MateriaId);
            if (materia is null)
                return Results.BadRequest(new { mensaje = "La materia indicada no existe." });
            var periodoMateria = CodigoPeriodo(materia);
            if (await context.DocentesMaterias.AnyAsync(dm => dm.IdDocente == dto.DocenteId &&
                dm.IdMateria == dto.MateriaId && dm.CicloLectivo == dto.CicloLectivo &&
                dm.Cuatrimestre == periodoMateria))
                return Results.Conflict(new { mensaje = "El docente ya está asignado a esa materia y período." });

            var asignacion = new DocenteMateria
            {
                IdDocente = dto.DocenteId,
                IdMateria = dto.MateriaId,
                CicloLectivo = dto.CicloLectivo,
                Cuatrimestre = periodoMateria
            };
            context.Add(asignacion);
            var inscripcionesSinDocente = await context.EstudianteMaterias
                .Where(i => i.IdMateria == dto.MateriaId &&
                    i.CicloLectivo == dto.CicloLectivo && i.IdDocente == null)
                .ToListAsync();
            inscripcionesSinDocente.ForEach(i => i.IdDocente = dto.DocenteId);
            await context.SaveChangesAsync();
            return Results.Created($"/api/gestion-academica/docentes-materias/{asignacion.Id}", asignacion);
        });

        group.MapGet("/carreras/{carreraId:int}/detalle", async (int carreraId,
            int? cicloLectivo, AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            if (ciclo is < 2020 or > 2100)
                return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });

            var carrera = await context.Set<Carrera>().AsNoTracking()
                .Where(c => c.IdCarrera == carreraId)
                .Select(c => new
                {
                    c.IdCarrera,
                    c.Nombre,
                    c.Tipo,
                    c.PlanEstudios,
                    c.DuracionAnios
                })
                .SingleOrDefaultAsync();

            if (carrera is null)
                return Results.NotFound(new { mensaje = "La carrera indicada no existe." });

            var materias = await context.Materias.AsNoTracking()
                .Where(m => m.AnioCursada != null && m.AnioCursada.IdCarrera == carreraId)
                .Include(m => m.AnioCursada)
                .Include(m => m.Horarios)
                .Include(m => m.Correlativas)
                .OrderBy(m => m.AnioCursada!.NumeroAnio)
                .ThenBy(m => m.Nombre)
                .ToListAsync();

            var materiaIds = materias.Select(m => m.IdMateria).ToList();
            var asignaciones = materiaIds.Count == 0
                ? []
                : await context.DocentesMaterias.AsNoTracking()
                    .Where(dm => materiaIds.Contains(dm.IdMateria) &&
                        dm.CicloLectivo == ciclo && dm.Activa)
                    .Include(dm => dm.Docente)
                    .ThenInclude(d => d.Persona)
                    .OrderBy(dm => dm.Docente.Persona.Apellido)
                    .ThenBy(dm => dm.Docente.Persona.Nombre)
                    .ToListAsync();

            var inscripciones = materiaIds.Count == 0
                ? []
                : await context.EstudianteMaterias.AsNoTracking()
                    .Where(i => materiaIds.Contains(i.IdMateria) &&
                        i.CicloLectivo == ciclo && i.Estado != "Cancelada")
                    .Include(i => i.Estudiante)
                    .ThenInclude(e => e!.Persona)
                    .OrderBy(i => i.Estudiante!.Persona.Apellido)
                    .ThenBy(i => i.Estudiante!.Persona.Nombre)
                    .ToListAsync();

            var materiasConActividad = asignaciones.Select(a => a.IdMateria)
                .Concat(inscripciones.Select(i => i.IdMateria))
                .ToHashSet();
            var materiasVisibles = ciclo == DateTime.UtcNow.Year
                ? materias
                : materias.Where(m => materiasConActividad.Contains(m.IdMateria)).ToList();

            var anios = materiasVisibles
                .GroupBy(m => new
                {
                    m.AnioCursada!.IdAnio,
                    m.AnioCursada.NumeroAnio,
                    m.AnioCursada.NombreAnio
                })
                .OrderBy(g => g.Key.NumeroAnio)
                .Select(g => new
                {
                    g.Key.IdAnio,
                    g.Key.NumeroAnio,
                    g.Key.NombreAnio,
                    Materias = g.Select(m => new
                    {
                        m.IdMateria,
                        m.Nombre,
                        m.Modalidad,
                        m.Estado,
                        m.TipoCursada,
                        m.NumeroPeriodo,
                        Horarios = m.Horarios
                            .OrderBy(h => h.DiaSemana)
                            .ThenBy(h => h.HoraInicio)
                            .Select(h => new
                            {
                                h.DiaSemana,
                                h.HoraInicio,
                                h.HoraFin
                            }),
                        Correlativas = m.Correlativas
                            .OrderBy(c => c.Nombre)
                            .Select(c => new { c.IdMateria, c.Nombre }),
                        Profesores = asignaciones
                            .Where(dm => dm.IdMateria == m.IdMateria)
                            .Select(dm => new
                            {
                                dm.IdDocente,
                                Nombre = $"{dm.Docente.Persona.Nombre} {dm.Docente.Persona.Apellido}",
                                dm.Cuatrimestre
                            }),
                        Estudiantes = inscripciones
                            .Where(i => i.IdMateria == m.IdMateria)
                            .Select(i => new
                            {
                                i.IdEstudiante,
                                Nombre = i.Estudiante!.Persona.Nombre,
                                Apellido = i.Estudiante.Persona.Apellido,
                                i.Estudiante.Legajo,
                                i.Estado,
                                i.FechaInscripcion
                            })
                    })
                });

            return Results.Ok(new
            {
                carrera.IdCarrera,
                carrera.Nombre,
                carrera.Tipo,
                carrera.PlanEstudios,
                carrera.DuracionAnios,
                CicloLectivo = ciclo,
                TotalMaterias = materiasVisibles.Count,
                TotalEstudiantes = inscripciones.Select(i => i.IdEstudiante).Distinct().Count(),
                Anios = anios
            });
        });

        group.MapGet("/periodos-inscripcion", async (int? cicloLectivo, AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            var materias = await context.Materias.AsNoTracking()
                .Where(m => m.AnioCursada != null)
                .OrderBy(m => m.AnioCursada!.Carrera!.Nombre)
                .ThenBy(m => m.AnioCursada!.NumeroAnio)
                .ThenBy(m => m.Nombre)
                .Select(m => new
                {
                    MateriaId = m.IdMateria,
                    Materia = m.Nombre,
                    CarreraId = m.AnioCursada!.IdCarrera,
                    Carrera = m.AnioCursada.Carrera!.Nombre,
                    m.AnioCursada.NumeroAnio
                })
                .ToListAsync();
            var periodos = await context.PeriodosInscripcionMaterias.AsNoTracking()
                .Where(p => p.CicloLectivo == ciclo)
                .ToListAsync();
            var periodosEspecificos = periodos
                .Where(p => p.MateriaId.HasValue)
                .ToDictionary(p => p.MateriaId!.Value);
            var periodosAnteriores = periodos
                .Where(p => !p.MateriaId.HasValue)
                .ToDictionary(p => p.CarreraId);

            return Results.Ok(materias.Select(m =>
            {
                periodosEspecificos.TryGetValue(m.MateriaId, out var periodo);
                if (periodo is null)
                    periodosAnteriores.TryGetValue(m.CarreraId, out periodo);
                var ahora = DateTime.UtcNow;
                return new
                {
                    m.CarreraId,
                    m.Carrera,
                    m.MateriaId,
                    m.Materia,
                    m.NumeroAnio,
                    CicloLectivo = ciclo,
                    Configurado = periodo is not null,
                    FechaInicio = periodo?.FechaInicioUtc,
                    FechaFin = periodo?.FechaFinUtc,
                    Abierta = periodo is not null && ahora >= periodo.FechaInicioUtc && ahora <= periodo.FechaFinUtc
                };
            }));
        });

        group.MapPut("/periodos-inscripcion", () => Results.BadRequest(new
        {
            mensaje = "La inscripción ya no se configura por materia. Usá el período general de la carrera."
        }));

        group.MapGet("/periodos-inscripcion-carreras", async (int? cicloLectivo,
            AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            if (ciclo is < 2020 or > 2100)
                return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });

            var carreras = await context.Set<Carrera>().AsNoTracking()
                .OrderBy(c => c.Nombre)
                .Select(c => new { c.IdCarrera, c.Nombre, c.PlanEstudios })
                .ToListAsync();
            var periodos = await context.PeriodosInscripcionMaterias.AsNoTracking()
                .Where(p => p.CicloLectivo == ciclo && p.MateriaId == null)
                .ToDictionaryAsync(p => p.CarreraId);
            var ahora = DateTime.UtcNow;

            return Results.Ok(carreras.Select(carrera =>
            {
                periodos.TryGetValue(carrera.IdCarrera, out var periodo);
                return new
                {
                    carrera.IdCarrera,
                    Carrera = carrera.Nombre,
                    carrera.PlanEstudios,
                    CicloLectivo = ciclo,
                    Configurado = periodo is not null,
                    FechaInicio = periodo?.FechaInicioUtc,
                    FechaFin = periodo?.FechaFinUtc,
                    Abierta = periodo is not null && ahora >= periodo.FechaInicioUtc && ahora <= periodo.FechaFinUtc
                };
            }));
        });

        group.MapPut("/periodos-inscripcion-carreras", async (
            PeriodoInscripcionCarreraActualizarDto dto, HttpContext http,
            AppDbContext context) =>
        {
            var carrera = await context.Set<Carrera>().AsNoTracking()
                .SingleOrDefaultAsync(c => c.IdCarrera == dto.CarreraId);
            if (carrera is null)
                return Results.BadRequest(new { mensaje = "La carrera indicada no existe." });
            if (dto.CicloLectivo is < 2020 or > 2100)
                return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });

            var inicio = dto.FechaInicio.ToUniversalTime();
            var fin = dto.FechaFin.ToUniversalTime();
            if (fin <= inicio)
                return Results.BadRequest(new { mensaje = "La fecha de cierre debe ser posterior a la fecha de apertura." });

            var periodo = await context.PeriodosInscripcionMaterias.SingleOrDefaultAsync(p =>
                p.CarreraId == dto.CarreraId && p.CicloLectivo == dto.CicloLectivo &&
                p.MateriaId == null);
            if (periodo is null)
            {
                periodo = new PeriodoInscripcionMateria
                {
                    CarreraId = dto.CarreraId,
                    CicloLectivo = dto.CicloLectivo,
                    MateriaId = null
                };
                context.PeriodosInscripcionMaterias.Add(periodo);
            }

            var configuracionesAnteriores = await context.PeriodosInscripcionMaterias
                .Where(p => p.CarreraId == dto.CarreraId &&
                    p.CicloLectivo == dto.CicloLectivo && p.MateriaId != null)
                .ToListAsync();
            context.PeriodosInscripcionMaterias.RemoveRange(configuracionesAnteriores);
            periodo.FechaInicioUtc = inicio;
            periodo.FechaFinUtc = fin;
            periodo.ModificadoPorUsuarioId = http.User.ObtenerUsuarioId();
            periodo.FechaModificacionUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();

            await NotificacionAutomaticaService.ReprogramarPeriodoMateriaAsync(
                periodo, http.User.ObtenerUsuarioId(), context);
            await context.SaveChangesAsync();
            return Results.Ok(new
            {
                mensaje = $"La inscripción a todas las materias de {carrera.Nombre} fue actualizada correctamente."
            });
        });

        group.MapGet("/periodo-inscripcion-examenes", async (int? cicloLectivo,
            AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            if (ciclo is < 2020 or > 2100)
                return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });

            var periodo = await context.PeriodosInscripcionExamenes.AsNoTracking()
                .SingleOrDefaultAsync(p => p.CicloLectivo == ciclo);
            var ahora = DateTime.UtcNow;
            return Results.Ok(new
            {
                CicloLectivo = ciclo,
                Configurado = periodo is not null,
                FechaInicio = periodo?.FechaInicioUtc,
                FechaFin = periodo?.FechaFinUtc,
                Abierta = periodo is not null && ahora >= periodo.FechaInicioUtc && ahora <= periodo.FechaFinUtc
            });
        });

        group.MapPut("/periodo-inscripcion-examenes", async (
            PeriodoInscripcionExamenActualizarDto dto, HttpContext http, AppDbContext context) =>
        {
            if (dto.CicloLectivo is < 2020 or > 2100)
                return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });

            var inicio = dto.FechaInicio.ToUniversalTime();
            var fin = dto.FechaFin.ToUniversalTime();
            if (fin <= inicio)
                return Results.BadRequest(new { mensaje = "La fecha de cierre debe ser posterior a la fecha de apertura." });

            var periodo = await context.PeriodosInscripcionExamenes
                .SingleOrDefaultAsync(p => p.CicloLectivo == dto.CicloLectivo);
            if (periodo is null)
            {
                periodo = new PeriodoInscripcionExamen { CicloLectivo = dto.CicloLectivo };
                context.PeriodosInscripcionExamenes.Add(periodo);
            }

            periodo.FechaInicioUtc = inicio;
            periodo.FechaFinUtc = fin;
            periodo.ModificadoPorUsuarioId = http.User.ObtenerUsuarioId();
            periodo.FechaModificacionUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await NotificacionAutomaticaService.ReprogramarPeriodoExamenesAsync(
                periodo, http.User.ObtenerUsuarioId(), context);
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Período de inscripción a exámenes finales actualizado correctamente." });
        });
    }

    private static string CodigoPeriodo(Materia materia) => materia.TipoCursada switch
    {
        "Cuatrimestral" => $"{materia.NumeroPeriodo ?? 1}C",
        "Bimestral" => $"{materia.NumeroPeriodo ?? 1}B",
        _ => "Anual"
    };
}
