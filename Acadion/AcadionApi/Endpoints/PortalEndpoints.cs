using AcadionApi.Datos;
using AcadionApi.DTOs;
using AcadionApi.Logica;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class PortalEndpoints
{
    private static readonly string[] TiposAsistencia = ["Presente", "Ausente", "Tardanza"];
    private static readonly string[] TiposExamen = ["Parcial", "Final", "TrabajoPractico", "Presentacion", "Recuperatorio"];

    public static void MapPortalEndpoints(this IEndpointRouteBuilder routes)
    {
        var me = routes.MapGroup("/api/me").RequireAuthorization();

        me.MapGet("/perfil", async (HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var usuario = await context.Usuarios.AsNoTracking()
                .Include(u => u.Persona).Include(u => u.Rol).Include(u => u.Carrera)
                .SingleOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario is null) return Results.NotFound();

            var financiamiento = await context.PerfilesFinanciamiento.AsNoTracking()
                .SingleOrDefaultAsync(p => p.UsuarioId == usuarioId);
            var allegados = await context.Allegados.AsNoTracking()
                .Where(a => a.EstudianteId == usuarioId)
                .OrderBy(a => a.Id)
                .Select(a => new { a.Id, a.NombreApellido, a.Relacion, a.Telefono })
                .ToListAsync();
            var matriculaInicial = await context.MatriculasIniciales.AsNoTracking()
                .Where(m => m.EstudianteId == usuarioId)
                .OrderByDescending(m => m.PeriodoLectivo)
                .FirstOrDefaultAsync();
            var ahora = DateTime.UtcNow;
            var cuotaActual = await context.CuotasMensuales.AsNoTracking()
                .SingleOrDefaultAsync(c => c.EstudianteId == usuarioId &&
                    c.Anio == ahora.Year && c.Mes == ahora.Month);
            var habilitadoFinales = await EstaHabilitadoFinancieramenteAsync(usuarioId, context);

            return Results.Ok(new
            {
                usuario.Id,
                usuario.PersonaId,
                usuario.NombreUsuario,
                Nombre = usuario.Persona.Nombre,
                Apellido = usuario.Persona.Apellido,
                usuario.Persona.Dni,
                usuario.Persona.FechaNacimiento,
                usuario.Persona.Direccion,
                usuario.Persona.Localidad,
                usuario.Persona.CodigoPostal,
                EmailPersonal = usuario.Persona.Email,
                usuario.EmailInstitucional,
                usuario.TelefonoContacto,
                usuario.Legajo,
                usuario.Especialidad,
                usuario.TituloAcademico,
                Carrera = usuario.Carrera?.Nombre,
                FotoPerfilUrl = ConstruirUrlPublica(http, usuario.FotoPerfilUrl),
                usuario.PromedioGeneral,
                Rol = usuario.Rol.Nombre,
                Estado = usuario.Estado.ToString(),
                usuario.DebeCambiarPassword,
                Financiamiento = new
                {
                    AporteFamiliares = financiamiento?.AporteFamiliares ?? false,
                    PlanesSociales = financiamiento?.PlanesSociales ?? false,
                    Trabajo = financiamiento?.Trabajo ?? false,
                    Beca = financiamiento?.Beca ?? false,
                    OtraFuente = financiamiento?.OtraFuente ?? false
                },
                EstadoMatriculaInicial = matriculaInicial?.Estado.ToString() ?? EstadoPago.Pendiente.ToString(),
                PeriodoMatriculaInicial = matriculaInicial?.PeriodoLectivo,
                EstadoCuotaActual = cuotaActual?.Estado.ToString() ?? EstadoPago.Pendiente.ToString(),
                PeriodoCuotaActual = $"{ahora.Year}-{ahora.Month:D2}",
                HabilitadoFinales = habilitadoFinales,
                Allegados = allegados
            });
        });

        me.MapPost("/foto-perfil", async (IFormFile foto, HttpContext http,
            IWebHostEnvironment environment, AppDbContext context) =>
        {
            if (foto.Length == 0 || foto.Length > 5 * 1024 * 1024)
                return Results.BadRequest(new { mensaje = "Seleccioná una imagen de hasta 5 MB." });

            var extension = foto.ContentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => string.Empty
            };
            if (extension.Length == 0)
                return Results.BadRequest(new { mensaje = "La foto debe ser JPEG, PNG o WebP." });

            var usuarioId = http.User.ObtenerUsuarioId();
            var usuario = await context.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario is null) return Results.NotFound();

            var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            var uploadDirectory = Path.Combine(webRoot, "uploads", "perfiles");
            Directory.CreateDirectory(uploadDirectory);
            var fileName = $"perfil-{usuarioId}-{Guid.NewGuid():N}{extension}";
            var destination = Path.Combine(uploadDirectory, fileName);
            await using (var stream = File.Create(destination))
                await foto.CopyToAsync(stream);

            var previousRelativePath = usuario.FotoPerfilUrl;
            usuario.FotoPerfilUrl = $"/uploads/perfiles/{fileName}";
            await context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(previousRelativePath))
            {
                var previousName = Path.GetFileName(previousRelativePath);
                var previousPath = Path.Combine(uploadDirectory, previousName);
                if (File.Exists(previousPath)) File.Delete(previousPath);
            }

            return Results.Ok(new { fotoPerfilUrl = ConstruirUrlPublica(http, usuario.FotoPerfilUrl) });
        }).DisableAntiforgery();

        me.MapPut("/perfil", async (PerfilPropioActualizarDto dto,
            HttpContext http, AppDbContext context) =>
        {
            var email = dto.EmailPersonal?.Trim() ?? string.Empty;
            var telefono = dto.TelefonoContacto?.Trim() ?? string.Empty;
            var direccion = dto.Direccion?.Trim() ?? string.Empty;
            var localidad = dto.Localidad?.Trim() ?? string.Empty;
            if (email.Length > 160 || telefono.Length > 40 ||
                direccion.Length > 180 || localidad.Length > 100 || dto.CodigoPostal < 0)
                return Results.BadRequest(new { mensaje = "Los datos ingresados superan el tamaño permitido." });
            if (email.Length > 0 &&
                !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
                return Results.BadRequest(new { mensaje = "El correo electrónico no es válido." });

            var usuarioId = http.User.ObtenerUsuarioId();
            var usuario = await context.Usuarios.Include(u => u.Persona)
                .SingleOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario is null) return Results.NotFound();

            usuario.Persona.Email = email;
            usuario.TelefonoContacto = telefono;
            usuario.Persona.Direccion = direccion;
            usuario.Persona.Localidad = localidad;
            usuario.Persona.CodigoPostal = dto.CodigoPostal;
            await context.SaveChangesAsync();
            return Results.NoContent();
        });

        me.MapPut("/financiamiento", async (FinanciamientoPropioDto dto,
            HttpContext http, AppDbContext context) =>
        {
            if (!http.User.IsInRole(RolesSistema.Estudiante)) return Results.Forbid();
            var usuarioId = http.User.ObtenerUsuarioId();
            var perfil = await context.PerfilesFinanciamiento
                .SingleOrDefaultAsync(p => p.UsuarioId == usuarioId);
            if (perfil is null)
            {
                perfil = new PerfilFinanciamiento { UsuarioId = usuarioId };
                context.PerfilesFinanciamiento.Add(perfil);
            }
            perfil.AporteFamiliares = dto.AporteFamiliares;
            perfil.PlanesSociales = dto.PlanesSociales;
            perfil.Trabajo = dto.Trabajo;
            perfil.Beca = dto.Beca;
            perfil.OtraFuente = dto.OtraFuente;
            await context.SaveChangesAsync();
            return Results.NoContent();
        });

        me.MapPut("/allegados", async (AllegadosPropiosActualizarDto dto,
            HttpContext http, AppDbContext context) =>
        {
            if (!http.User.IsInRole(RolesSistema.Estudiante)) return Results.Forbid();
            var items = dto.Allegados ?? [];
            if (items.Count > 3)
                return Results.BadRequest(new { mensaje = "Podés registrar como máximo tres allegados." });
            if (items.Any(a => string.IsNullOrWhiteSpace(a.NombreApellido) ||
                string.IsNullOrWhiteSpace(a.Relacion) || a.NombreApellido.Length > 120 ||
                (a.Relacion?.Length ?? 0) > 60 || (a.Telefono?.Length ?? 0) > 30))
                return Results.BadRequest(new { mensaje = "Completá correctamente los datos de cada allegado." });

            var usuarioId = http.User.ObtenerUsuarioId();
            var existentes = await context.Allegados
                .Where(a => a.EstudianteId == usuarioId).ToListAsync();
            context.Allegados.RemoveRange(existentes);
            context.Allegados.AddRange(items.Select(a => new Allegado
            {
                EstudianteId = usuarioId,
                NombreApellido = a.NombreApellido.Trim(),
                Relacion = a.Relacion.Trim(),
                Telefono = a.Telefono?.Trim() ?? string.Empty
            }));
            await context.SaveChangesAsync();
            return Results.NoContent();
        });

        me.MapGet("/materias", async (HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            if (http.User.IsInRole(RolesSistema.Docente))
            {
                return Results.Ok(await context.DocentesMaterias.AsNoTracking()
                    .Where(dm => dm.IdDocente == usuarioId && dm.Activa)
                    .Include(dm => dm.Materia).ThenInclude(m => m.AnioCursada)!.ThenInclude(a => a!.Carrera)
                    .Include(dm => dm.Materia).ThenInclude(m => m.Horarios)
                    .Select(dm => new
                    {
                        AsignacionId = dm.Id,
                        MateriaId = dm.IdMateria,
                        dm.Materia.Nombre,
                        dm.Materia.Modalidad,
                        dm.Materia.Estado,
                        dm.CicloLectivo,
                        dm.Cuatrimestre,
                        Anio = dm.Materia.AnioCursada == null ? string.Empty : dm.Materia.AnioCursada.NombreAnio,
                        NumeroAnio = dm.Materia.AnioCursada == null ? 0 : dm.Materia.AnioCursada.NumeroAnio,
                        Carrera = dm.Materia.AnioCursada == null || dm.Materia.AnioCursada.Carrera == null
                            ? string.Empty
                            : dm.Materia.AnioCursada.Carrera.Nombre,
                        PlanEstudios = dm.Materia.AnioCursada == null || dm.Materia.AnioCursada.Carrera == null
                            ? string.Empty
                            : dm.Materia.AnioCursada.Carrera.PlanEstudios,
                        Horarios = dm.Materia.Horarios
                            .OrderBy(h => h.DiaSemana)
                            .ThenBy(h => h.HoraInicio)
                            .Select(h => new { h.DiaSemana, h.HoraInicio, h.HoraFin }),
                        CantidadEstudiantes = context.EstudianteMaterias.Count(em =>
                            em.IdMateria == dm.IdMateria && em.IdDocente == usuarioId &&
                            em.CicloLectivo == dm.CicloLectivo && em.Estado != "Cancelada")
                    }).ToListAsync());
            }

            return Results.Ok(await context.EstudianteMaterias.AsNoTracking()
                .Where(em => em.IdEstudiante == usuarioId && em.Estado != "Cancelada")
                .Include(em => em.Materia).ThenInclude(m => m!.Horarios)
                .Include(em => em.Materia).ThenInclude(m => m!.AnioCursada)!.ThenInclude(a => a!.Carrera)
                .Include(em => em.Docente).ThenInclude(d => d!.Persona)
                .OrderByDescending(em => em.CicloLectivo)
                .ThenBy(em => em.Materia!.Nombre)
                .Select(em => new
                {
                    InscripcionId = em.IdEstudianteMateria,
                    MateriaId = em.IdMateria,
                    em.Materia!.Nombre,
                    em.CicloLectivo,
                    em.Cuatrimestre,
                    em.Estado,
                    Modalidad = em.Materia!.Modalidad,
                    Anio = em.Materia.AnioCursada == null ? string.Empty : em.Materia.AnioCursada.NombreAnio,
                    NumeroAnio = em.Materia.AnioCursada == null ? 0 : em.Materia.AnioCursada.NumeroAnio,
                    Carrera = em.Materia.AnioCursada == null || em.Materia.AnioCursada.Carrera == null
                        ? string.Empty
                        : em.Materia.AnioCursada.Carrera.Nombre,
                    Horarios = em.Materia.Horarios
                        .OrderBy(h => h.DiaSemana)
                        .ThenBy(h => h.HoraInicio)
                        .Select(h => new { h.DiaSemana, h.HoraInicio, h.HoraFin }),
                    InicioDictado = context.Set<Asistencia>()
                        .Where(a => a.IdEstudianteMateria == em.IdEstudianteMateria)
                        .Select(a => (DateTime?)a.Fecha)
                        .Min(),
                    Docente = em.Docente == null ? null : em.Docente.Persona.Nombre + " " + em.Docente.Persona.Apellido
                }).ToListAsync());
        });

        me.MapGet("/evaluaciones", async (HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var inscripciones = context.EstudianteMaterias.AsNoTracking().Where(i =>
                i.IdEstudiante == usuarioId && i.Estado != "Cancelada");
            return Results.Ok(await context.Set<Examen>().AsNoTracking()
                .Where(e => inscripciones.Any(i => i.IdMateria == e.IdMateria &&
                    i.CicloLectivo == e.CicloLectivo && i.IdDocente == e.IdDocente))
                .Where(e => e.TipoExamen != "Final" || context.InscripcionesExamenes.Any(i =>
                    i.ExamenId == e.IdExamen && i.EstudianteId == usuarioId && i.Estado == "Inscripto"))
                .OrderByDescending(e => e.Fecha)
                .Select(e => new
                {
                    e.IdExamen,
                    e.IdMateria,
                    Materia = e.Materia!.Nombre,
                    e.Fecha,
                    e.TipoExamen,
                    e.CicloLectivo,
                    e.NotaMinimaRegularizacion,
                    e.NotaMinimaPromocion
                }).ToListAsync());
        });

        me.MapGet("/resumen-academico", async (HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var inscripciones = await context.EstudianteMaterias.AsNoTracking()
                .Where(i => i.IdEstudiante == usuarioId && i.Estado != "Cancelada")
                .Include(i => i.Materia)
                .OrderByDescending(i => i.CicloLectivo)
                .ThenBy(i => i.Materia!.Nombre)
                .ToListAsync();
            var claves = inscripciones.Select(i => new { i.IdMateria, i.CicloLectivo, i.IdDocente }).ToList();
            var materiaIds = claves.Select(c => c.IdMateria).Distinct().ToList();
            var ciclos = claves.Select(c => c.CicloLectivo).Distinct().ToList();
            var examenes = await context.Set<Examen>().AsNoTracking()
                .Where(e => materiaIds.Contains(e.IdMateria) && ciclos.Contains(e.CicloLectivo))
                .ToListAsync();
            var notas = await context.Set<NotaExamen>().AsNoTracking()
                .Where(n => n.IdEstudiante == usuarioId)
                .ToListAsync();
            var finalesInscriptos = await context.InscripcionesExamenes.AsNoTracking()
                .Where(i => i.EstudianteId == usuarioId && i.Estado == "Inscripto")
                .Select(i => i.ExamenId)
                .ToListAsync();
            var notasPorExamen = notas.ToDictionary(n => n.IdExamen);
            var finalesPermitidos = finalesInscriptos.ToHashSet();

            var resumen = inscripciones.Select(inscripcion =>
            {
                var evaluaciones = examenes.Where(e => e.IdMateria == inscripcion.IdMateria &&
                    e.CicloLectivo == inscripcion.CicloLectivo && e.IdDocente == inscripcion.IdDocente).ToList();
                var evaluacionesCursada = evaluaciones.Where(e => !EsFinal(e)).ToList();
                var notasCursada = evaluacionesCursada
                    .Where(e => notasPorExamen.ContainsKey(e.IdExamen))
                    .Select(e => notasPorExamen[e.IdExamen])
                    .ToList();
                var promociono = evaluacionesCursada.Count > 0 &&
                    notasCursada.Count == evaluacionesCursada.Count &&
                    notasCursada.All(n => n.Condicion == "Promocionó");
                var regularizo = evaluacionesCursada.Count > 0 &&
                    notasCursada.Count == evaluacionesCursada.Count &&
                    evaluacionesCursada.All(e =>
                        notasPorExamen[e.IdExamen].Nota >= e.NotaMinimaRegularizacion);
                var finalesAprobados = evaluaciones
                    .Where(EsFinal)
                    .Where(e => finalesPermitidos.Contains(e.IdExamen) && notasPorExamen.ContainsKey(e.IdExamen))
                    .Select(e => new { Examen = e, Nota = notasPorExamen[e.IdExamen] })
                    .Where(x => x.Nota.Nota >= x.Examen.NotaMinimaRegularizacion)
                    .OrderByDescending(x => x.Examen.Fecha)
                    .ToList();
                var finalAprobado = finalesAprobados.FirstOrDefault();
                var promedioPromocion = promociono ? Math.Round(notasCursada.Average(n => n.Nota), 2) : (decimal?)null;
                var notaFinal = finalAprobado?.Nota.Nota ?? promedioPromocion;
                var estadoGuardado = inscripcion.Estado is "Aprobada" or "Promocionada"
                    ? "En curso"
                    : inscripcion.Estado;
                var estado = finalAprobado is not null
                    ? "Aprobada"
                    : promociono ? "Promocionada" : regularizo ? "Regular" : estadoGuardado;
                var via = finalAprobado is not null ? "Final" : promociono ? "Promoción" : string.Empty;
                return new
                {
                    InscripcionId = inscripcion.IdEstudianteMateria,
                    MateriaId = inscripcion.IdMateria,
                    Materia = inscripcion.Materia!.Nombre,
                    inscripcion.CicloLectivo,
                    Estado = estado,
                    ViaAprobacion = via,
                    CalificacionFinal = notaFinal,
                    PromedioEvaluaciones = promedioPromocion,
                    EvaluacionesCreadas = evaluacionesCursada.Count,
                    EvaluacionesCalificadas = notasCursada.Count,
                    FechaAprobacion = finalAprobado?.Examen.Fecha,
                    TipoAprobacion = finalAprobado is not null ? "Examen final" : promociono ? "Promoción directa" : null
                };
            });
            return Results.Ok(resumen);
        });

        me.MapGet("/periodo-inscripcion-materias", async (HttpContext http, AppDbContext context) =>
        {
            if (!http.User.IsInRole(RolesSistema.Estudiante)) return Results.Forbid();
            var usuarioId = http.User.ObtenerUsuarioId();
            var carrera = await context.Usuarios.AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => new { u.CarreraId, Carrera = u.Carrera == null ? "" : u.Carrera.Nombre })
                .SingleAsync();
            if (!carrera.CarreraId.HasValue)
                return Results.BadRequest(new { mensaje = "Tu cuenta todavía no tiene una carrera asignada. Contactá a Secretaría." });

            var ciclo = DateTime.UtcNow.Year;
            var cantidadMaterias = await context.Materias.AsNoTracking()
                .CountAsync(m => m.AnioCursada != null &&
                    m.AnioCursada.IdCarrera == carrera.CarreraId.Value && m.Estado == "Activa");
            var periodo = await context.PeriodosInscripcionMaterias.AsNoTracking()
                .SingleOrDefaultAsync(p => p.CicloLectivo == ciclo &&
                    p.CarreraId == carrera.CarreraId.Value && p.MateriaId == null);
            if (periodo is null)
                return Results.Ok(new { carrera.Carrera, CicloLectivo = ciclo, Configurado = false, Abierta = false });

            var ahora = DateTime.UtcNow;
            var abierta = ahora >= periodo.FechaInicioUtc && ahora <= periodo.FechaFinUtc;
            return Results.Ok(new
            {
                carrera.Carrera,
                CicloLectivo = ciclo,
                Configurado = true,
                FechaInicio = periodo.FechaInicioUtc,
                FechaFin = periodo.FechaFinUtc,
                Abierta = abierta,
                MateriasConInscripcionAbierta = abierta ? cantidadMaterias : 0
            });
        }).RequierePermiso(PermisosSistema.InscripcionesPropiasLeer);

        me.MapGet("/materias-disponibles", async (HttpContext http, AppDbContext context) =>
        {
            if (!http.User.IsInRole(RolesSistema.Estudiante)) return Results.Forbid();

            var usuarioId = http.User.ObtenerUsuarioId();
            var cicloLectivo = DateTime.UtcNow.Year;
            var carreraId = await context.Usuarios.AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => u.CarreraId)
                .SingleAsync();
            if (!carreraId.HasValue)
                return Results.BadRequest(new { mensaje = "Tu cuenta todavía no tiene una carrera asignada. Contactá a Secretaría." });
            var inscriptas = await context.EstudianteMaterias.AsNoTracking()
                .Where(em => em.IdEstudiante == usuarioId && em.CicloLectivo == cicloLectivo)
                .Select(em => em.IdMateria)
                .ToListAsync();
            var estadosQueHabilitan = new[]
            {
                "Regular", "Regularizada", "Regularizado", "Aprobada", "Aprobado", "Promocionada", "Promocionado"
            };
            var regularizadas = await context.EstudianteMaterias.AsNoTracking()
                .Where(em => em.IdEstudiante == usuarioId &&
                    estadosQueHabilitan.Contains(em.Estado))
                .Select(em => em.IdMateria)
                .Distinct()
                .ToListAsync();

            var materias = await context.Materias.AsNoTracking()
                .Where(m => m.Estado == "Activa" &&
                    m.AnioCursada!.IdCarrera == carreraId.Value &&
                    !inscriptas.Contains(m.IdMateria))
                .Include(m => m.Correlativas)
                .Include(m => m.Horarios)
                .OrderBy(m => m.AnioCursada!.NumeroAnio)
                .ThenBy(m => m.Nombre)
                .ToListAsync();
            var periodo = await context.PeriodosInscripcionMaterias.AsNoTracking()
                .SingleOrDefaultAsync(p => p.CicloLectivo == cicloLectivo &&
                    p.CarreraId == carreraId.Value && p.MateriaId == null);
            var ahora = DateTime.UtcNow;
            if (periodo is null || ahora < periodo.FechaInicioUtc || ahora > periodo.FechaFinUtc)
                materias.Clear();
            var idsMaterias = materias.Select(m => m.IdMateria).ToList();
            var asignaciones = await context.DocentesMaterias.AsNoTracking()
                .Where(dm => dm.CicloLectivo == cicloLectivo && dm.Activa &&
                    idsMaterias.Contains(dm.IdMateria))
                .Include(dm => dm.Docente).ThenInclude(d => d.Persona)
                .OrderBy(dm => dm.Id)
                .ToListAsync();
            var asignacionesPorMateria = asignaciones
                .GroupBy(a => a.IdMateria)
                .ToDictionary(g => g.Key, g => g.First());

            var disponibles = materias.Select(materia =>
                {
                    asignacionesPorMateria.TryGetValue(materia.IdMateria, out var asignacion);
                    var pendientes = materia.Correlativas
                        .Where(c => !regularizadas.Contains(c.IdMateria))
                        .Select(c => c.Nombre)
                        .OrderBy(nombre => nombre)
                        .ToArray();
                    return new
                    {
                        MateriaId = materia.IdMateria,
                        materia.Nombre,
                        materia.Modalidad,
                        Docente = asignacion is null ? null : $"{asignacion.Docente.Persona.Nombre} {asignacion.Docente.Persona.Apellido}".Trim(),
                        CicloLectivo = cicloLectivo,
                        Cuatrimestre = asignacion?.Cuatrimestre ?? "A confirmar",
                        Habilitada = pendientes.Length == 0,
                        CorrelativasPendientes = pendientes,
                        Horarios = materia.Horarios
                            .OrderBy(h => h.DiaSemana)
                            .ThenBy(h => h.HoraInicio)
                            .Select(h => new { h.DiaSemana, h.HoraInicio, h.HoraFin })
                    };
                });
            return Results.Ok(disponibles);
        }).RequierePermiso(PermisosSistema.InscripcionesPropiasLeer);

        me.MapPost("/inscripciones", async (InscripcionPropiaCrearDto dto,
            HttpContext http, AppDbContext context, IEstudianteMateriaLogica logica) =>
        {
            if (!http.User.IsInRole(RolesSistema.Estudiante)) return Results.Forbid();
            if (dto.MateriaId <= 0)
                return Results.BadRequest(new { mensaje = "La materia es obligatoria." });

            var cicloLectivo = DateTime.UtcNow.Year;
            var carreraId = await context.Usuarios.AsNoTracking()
                .Where(u => u.Id == http.User.ObtenerUsuarioId())
                .Select(u => u.CarreraId)
                .SingleAsync();
            if (!carreraId.HasValue)
                return Results.BadRequest(new { mensaje = "Tu cuenta todavía no tiene una carrera asignada." });
            var materia = await context.Materias.AsNoTracking()
                .Where(m => m.IdMateria == dto.MateriaId && m.AnioCursada != null)
                .Select(m => new { m.IdMateria, m.Nombre, m.AnioCursada!.IdCarrera })
                .SingleOrDefaultAsync();
            if (materia is null || materia.IdCarrera != carreraId.Value)
                return Results.BadRequest(new { mensaje = "La materia no pertenece a tu carrera." });
            var periodo = await context.PeriodosInscripcionMaterias.AsNoTracking()
                .Where(p => p.CicloLectivo == cicloLectivo && p.CarreraId == carreraId.Value &&
                    p.MateriaId == null)
                .FirstOrDefaultAsync();
            var ahora = DateTime.UtcNow;
            if (periodo is null || ahora < periodo.FechaInicioUtc || ahora > periodo.FechaFinUtc)
                return Results.BadRequest(new { mensaje = $"La inscripción a {materia.Nombre} no está habilitada en este momento." });

            var asignacion = await context.DocentesMaterias.AsNoTracking()
                .Where(dm => dm.IdMateria == dto.MateriaId &&
                    dm.CicloLectivo == cicloLectivo && dm.Activa)
                .OrderBy(dm => dm.Id)
                .FirstOrDefaultAsync();
            try
            {
                var resultado = await logica.InscribirEstudianteAsync(new InscripcionCrearDto
                {
                    IdEstudiante = http.User.ObtenerUsuarioId(),
                    IdMateria = dto.MateriaId,
                    IdDocente = asignacion?.IdDocente,
                    CicloLectivo = cicloLectivo,
                    Cuatrimestre = asignacion?.Cuatrimestre ?? "Anual"
                });
                if (asignacion is not null)
                {
                    var examenesProgramados = await context.Set<Examen>().AsNoTracking()
                        .Include(e => e.Materia)
                        .Where(e => e.IdMateria == dto.MateriaId && e.CicloLectivo == cicloLectivo &&
                            e.IdDocente == asignacion.IdDocente && e.TipoExamen != "Final")
                        .ToListAsync();
                    foreach (var examen in examenesProgramados)
                        NotificacionAutomaticaService.ProgramarRecordatorioExamen(
                            examen, http.User.ObtenerUsuarioId(), context);
                    await context.SaveChangesAsync();
                }
                return Results.Created($"/api/me/inscripciones/{resultado.IdEstudianteMateria}", resultado);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { mensaje = ex.Message });
            }
        }).RequierePermiso(PermisosSistema.InscripcionesPropiasLeer);

        me.MapGet("/asistencias", async (HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            return Results.Ok(await context.Set<Asistencia>().AsNoTracking()
                .Where(a => a.Inscripcion!.IdEstudiante == usuarioId)
                .Include(a => a.Inscripcion).ThenInclude(i => i!.Materia)
                .OrderByDescending(a => a.Fecha)
                .Select(a => new
                {
                    a.IdAsistencia,
                    a.Fecha,
                    a.Tipo,
                    a.TipoClase,
                    a.TemaDictado,
                    a.Justificada,
                    a.CantidadInasistencias,
                    a.Observaciones,
                    MateriaId = a.Inscripcion!.IdMateria,
                    Materia = a.Inscripcion.Materia!.Nombre,
                    a.Inscripcion.CicloLectivo
                }).ToListAsync());
        });

        me.MapGet("/examenes", async (HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var habilitadoFinancieramente = await EstaHabilitadoFinancieramenteAsync(usuarioId, context);
            var ahora = DateTime.UtcNow;
            var ahoraArgentina = ObtenerAhoraArgentina();
            var inscripciones = context.EstudianteMaterias.Where(em =>
                em.IdEstudiante == usuarioId && em.Estado != "Cancelada");
            var examenes = await context.Set<Examen>().AsNoTracking()
                .Where(e => e.TipoExamen == "Final")
                .Where(e => inscripciones.Any(i => i.IdMateria == e.IdMateria &&
                    i.CicloLectivo == e.CicloLectivo && i.IdDocente == e.IdDocente))
                .Include(e => e.Materia)
                .OrderBy(e => e.Fecha)
                .Select(e => new
                {
                    e.IdExamen,
                    e.IdMateria,
                    Materia = e.Materia!.Nombre,
                    e.Fecha,
                    e.TipoExamen,
                    e.CicloLectivo,
                    Inscripto = context.InscripcionesExamenes.Any(i =>
                        i.ExamenId == e.IdExamen && i.EstudianteId == usuarioId),
                    PeriodoConfigurado = context.PeriodosInscripcionExamenes.Any(p =>
                        p.CicloLectivo == e.CicloLectivo),
                    PeriodoAbierto = context.PeriodosInscripcionExamenes.Any(p =>
                        p.CicloLectivo == e.CicloLectivo && ahora >= p.FechaInicioUtc && ahora <= p.FechaFinUtc)
                }).ToListAsync();

            return Results.Ok(examenes.Select(e => new
            {
                e.IdExamen,
                e.IdMateria,
                e.Materia,
                e.Fecha,
                e.TipoExamen,
                e.CicloLectivo,
                e.Inscripto,
                PuedeDarseDeBaja = e.Inscripto &&
                    ahoraArgentina < ConvertirAFechaArgentina(e.Fecha).AddHours(-24),
                LimiteBaja = ConvertirAFechaArgentina(e.Fecha).AddHours(-24),
                Habilitado = e.PeriodoAbierto && habilitadoFinancieramente,
                MotivoBloqueo = !e.PeriodoConfigurado
                    ? "La secretaría todavía no configuró el período de inscripción a finales."
                    : !e.PeriodoAbierto
                        ? "El período de inscripción a exámenes finales está cerrado."
                        : !habilitadoFinancieramente
                            ? "Para inscribirte tenés que tener la matrícula y las cuotas al día."
                            : null
            }));
        });

        me.MapPost("/examenes/{examenId:int}/inscripcion", async (int examenId,
            HttpContext http, AppDbContext context) =>
        {
            if (!http.User.IsInRole(RolesSistema.Estudiante)) return Results.Forbid();
            var usuarioId = http.User.ObtenerUsuarioId();
            var examen = await context.Set<Examen>().AsNoTracking()
                .Include(e => e.Materia)
                .SingleOrDefaultAsync(e => e.IdExamen == examenId);
            if (examen is null) return Results.NotFound(new { mensaje = "El examen no existe." });
            if (!examen.TipoExamen.Equals("Final", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { mensaje = "Solo es posible inscribirse a exámenes finales." });

            var pertenece = await context.EstudianteMaterias.AsNoTracking().AnyAsync(i =>
                i.IdEstudiante == usuarioId && i.IdMateria == examen.IdMateria &&
                i.CicloLectivo == examen.CicloLectivo && i.IdDocente == examen.IdDocente &&
                i.Estado != "Cancelada");
            if (!pertenece)
                return Results.BadRequest(new { mensaje = "No estás inscripto en la materia de este examen." });

            var ahora = DateTime.UtcNow;
            var periodoAbierto = await context.PeriodosInscripcionExamenes.AsNoTracking().AnyAsync(p =>
                p.CicloLectivo == examen.CicloLectivo && ahora >= p.FechaInicioUtc && ahora <= p.FechaFinUtc);
            if (!periodoAbierto)
                return Results.Conflict(new { mensaje = "El período de inscripción a exámenes finales está cerrado." });
            if (!await EstaHabilitadoFinancieramenteAsync(usuarioId, context))
                return Results.Conflict(new
                {
                    mensaje = "No podés inscribirte: la matrícula inicial o alguna cuota está pendiente."
                });

            if (await context.InscripcionesExamenes.AnyAsync(i =>
                i.ExamenId == examenId && i.EstudianteId == usuarioId))
                return Results.Conflict(new { mensaje = "Ya estás inscripto en este examen." });

            var inscripcion = new InscripcionExamen
            {
                ExamenId = examenId,
                EstudianteId = usuarioId
            };
            context.InscripcionesExamenes.Add(inscripcion);
            NotificacionAutomaticaService.ProgramarRecordatorioExamen(examen, usuarioId, context);
            await context.SaveChangesAsync();
            return Results.Created($"/api/me/examenes/{examenId}/inscripcion", new
            {
                inscripcion.Id,
                inscripcion.Estado,
                inscripcion.FechaInscripcionUtc
            });
        });

        me.MapDelete("/examenes/{examenId:int}/inscripcion", async (int examenId,
            HttpContext http, AppDbContext context) =>
        {
            if (!http.User.IsInRole(RolesSistema.Estudiante)) return Results.Forbid();

            var usuarioId = http.User.ObtenerUsuarioId();
            var inscripcion = await context.InscripcionesExamenes
                .Include(i => i.Examen)
                .SingleOrDefaultAsync(i => i.ExamenId == examenId && i.EstudianteId == usuarioId);
            if (inscripcion is null)
                return Results.NotFound(new { mensaje = "No estás inscripto en este examen." });
            if (!EsFinal(inscripcion.Examen))
                return Results.BadRequest(new { mensaje = "Solo es posible gestionar inscripciones a exámenes finales." });

            var limiteBaja = ConvertirAFechaArgentina(inscripcion.Examen.Fecha).AddHours(-24);
            if (ObtenerAhoraArgentina() >= limiteBaja)
            {
                return Results.Conflict(new
                {
                    mensaje = $"El plazo para darte de baja finalizó el {limiteBaja:dd/MM/yyyy 'a las' HH:mm}."
                });
            }

            context.InscripcionesExamenes.Remove(inscripcion);
            await context.NotificacionesGenerales
                .Where(n => n.ClaveAutomatica == $"AUTO:EXAMEN:{examenId}:ESTUDIANTE:{usuarioId}")
                .ExecuteDeleteAsync();
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Te diste de baja del examen correctamente." });
        });

        me.MapGet("/notas", async (HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            return Results.Ok(await context.Set<NotaExamen>().AsNoTracking()
                .Where(n => n.IdEstudiante == usuarioId)
                .Include(n => n.Examen).ThenInclude(e => e!.Materia)
                .OrderByDescending(n => n.Examen!.Fecha)
                .Select(n => new
                {
                    n.IdNota,
                    n.Nota,
                    n.Observaciones,
                    n.IdExamen,
                    Examen = n.Examen!.TipoExamen,
                    n.Examen.Fecha,
                    n.Examen.CicloLectivo,
                    MateriaId = n.Examen.IdMateria,
                    Materia = n.Examen.Materia!.Nombre,
                    Descripcion = n.Observaciones,
                    Resultado = n.Condicion,
                    CorregidoPor = n.Examen.Docente == null
                        ? null
                        : n.Examen.Docente.Persona.Nombre + " " + n.Examen.Docente.Persona.Apellido
                }).ToListAsync());
        });

        me.MapGet("/asistencia-personal", async (HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            return Results.Ok(await context.RegistrosAsistenciaPersonal.AsNoTracking()
                .Where(r => r.UsuarioId == usuarioId)
                .OrderByDescending(r => r.Fecha)
                .Select(r => new
                {
                    r.Id,
                    r.Fecha,
                    r.Estado,
                    r.Observaciones,
                    MateriaId = r.MateriaId,
                    Materia = r.Materia == null ? "Registro anterior" : r.Materia.Nombre
                })
                .ToListAsync());
        });

        MapDocencia(routes);
    }

    private static void MapDocencia(IEndpointRouteBuilder routes)
    {
        var docente = routes.MapGroup("/api/docente")
            .RequierePermiso(PermisosSistema.DocenciaGestionar);

        docente.MapGet("/alumnos", async (int? materiaId, int? cicloLectivo,
            HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var consulta = context.EstudianteMaterias.AsNoTracking()
                .Where(em => em.IdDocente == usuarioId)
                .Include(em => em.Estudiante).ThenInclude(e => e!.Persona)
                .Include(em => em.Materia).AsQueryable();
            if (materiaId.HasValue) consulta = consulta.Where(em => em.IdMateria == materiaId);
            if (cicloLectivo.HasValue) consulta = consulta.Where(em => em.CicloLectivo == cicloLectivo);

            var alumnos = await consulta
                .OrderBy(em => em.Estudiante!.Persona.Apellido)
                .ThenBy(em => em.Estudiante!.Persona.Nombre)
                .Select(em => new
            {
                em.IdEstudianteMateria,
                em.IdEstudiante,
                Estudiante = em.Estudiante!.Persona.Apellido + ", " + em.Estudiante.Persona.Nombre,
                em.IdMateria,
                Materia = em.Materia!.Nombre,
                em.CicloLectivo,
                em.Cuatrimestre,
                em.Estado,
                em.Estudiante.Legajo,
                em.Estudiante.FotoPerfilUrl
            }).ToListAsync();
            return Results.Ok(alumnos.Select(alumno => new
            {
                alumno.IdEstudianteMateria,
                alumno.IdEstudiante,
                alumno.Estudiante,
                alumno.IdMateria,
                alumno.Materia,
                alumno.CicloLectivo,
                alumno.Cuatrimestre,
                alumno.Estado,
                alumno.Legajo,
                FotoPerfilUrl = ConstruirUrlPublica(http, alumno.FotoPerfilUrl)
            }));
        });

        docente.MapGet("/asistencias", async (int materiaId, DateTime fecha,
            HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var asignado = await context.DocentesMaterias.AsNoTracking().AnyAsync(dm =>
                dm.IdDocente == usuarioId && dm.IdMateria == materiaId && dm.Activa);
            if (!asignado) return Results.Forbid();

            var dia = fecha.Date;
            return Results.Ok(await context.Set<Asistencia>().AsNoTracking()
                .Where(a => a.IdDocente == usuarioId && a.Fecha == dia &&
                    a.Inscripcion != null && a.Inscripcion.IdMateria == materiaId)
                .Select(a => new
                {
                    a.IdAsistencia,
                    a.IdEstudianteMateria,
                    a.Fecha,
                    a.Tipo,
                    a.TipoClase,
                    a.TemaDictado,
                    a.CantidadInasistencias,
                    a.Observaciones
                })
                .ToListAsync());
        });

        docente.MapPost("/asistencias", async (AsistenciaCrearDto dto,
            HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var inscripcion = await context.EstudianteMaterias
                .SingleOrDefaultAsync(i => i.IdEstudianteMateria == dto.IdEstudianteMateria);
            if (inscripcion is null) return Results.NotFound(new { mensaje = "La inscripción no existe." });
            if (inscripcion.IdDocente != usuarioId) return Results.Forbid();
            if (!TiposAsistencia.Contains(dto.Tipo, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(new { mensaje = "Tipo de asistencia inválido." });
            if (dto.TipoClase is not ("Presencial" or "Virtual"))
                return Results.BadRequest(new { mensaje = "El tipo de clase debe ser Presencial o Virtual." });
            if (dto.CantidadInasistencias is < 1 or > 10)
                return Results.BadRequest(new { mensaje = "La cantidad de inasistencias debe estar entre 1 y 10." });

            var fecha = dto.Fecha.Date;
            var existente = await context.Set<Asistencia>().SingleOrDefaultAsync(a =>
                a.IdEstudianteMateria == dto.IdEstudianteMateria && a.Fecha == fecha);
            if (existente is null)
            {
                existente = new Asistencia
                {
                    IdEstudianteMateria = dto.IdEstudianteMateria,
                    IdDocente = usuarioId,
                    Fecha = fecha
                };
                context.Add(existente);
            }
            existente.Tipo = dto.Tipo;
            existente.TipoClase = dto.TipoClase;
            existente.TemaDictado = string.Empty;
            existente.CantidadInasistencias = dto.Tipo.Equals("Ausente", StringComparison.OrdinalIgnoreCase)
                ? dto.CantidadInasistencias
                : 0;
            if (!dto.Tipo.Equals("Ausente", StringComparison.OrdinalIgnoreCase))
                existente.Justificada = false;
            existente.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones)
                ? null
                : dto.Observaciones.Trim();
            await context.SaveChangesAsync();
            return Results.Ok(new { existente.IdAsistencia });
        });

        docente.MapPost("/examenes", async (ExamenCrearDto dto,
            HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var asignado = await context.DocentesMaterias.AnyAsync(dm =>
                dm.IdDocente == usuarioId && dm.IdMateria == dto.IdMateria &&
                dm.CicloLectivo == dto.CicloLectivo && dm.Activa);
            if (!asignado) return Results.Forbid();
            if (!TiposExamen.Contains(dto.TipoExamen, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(new { mensaje = "Tipo de evaluación inválido." });

            var examen = new Examen
            {
                IdMateria = dto.IdMateria,
                IdDocente = usuarioId,
                CicloLectivo = dto.CicloLectivo,
                Fecha = dto.Fecha,
                TipoExamen = dto.TipoExamen,
                NotaMinimaRegularizacion = 6
            };
            context.Add(examen);
            await context.SaveChangesAsync();
            await NotificacionAutomaticaService.ReprogramarExamenAsync(examen.IdExamen, context);
            var estudiantes = await context.EstudianteMaterias.AsNoTracking()
                .Where(i => i.IdMateria == examen.IdMateria && i.IdDocente == examen.IdDocente &&
                    i.CicloLectivo == examen.CicloLectivo && i.Estado != "Cancelada")
                .Select(i => i.IdEstudiante)
                .Distinct()
                .ToListAsync();
            foreach (var estudianteId in estudiantes)
                await ActualizarEstadoMateriaAsync(estudianteId, examen, context);
            await context.SaveChangesAsync();
            return Results.Created($"/examenes/{examen.IdExamen}", new { examen.IdExamen });
        });

        docente.MapGet("/examenes", async (int? materiaId, int? cicloLectivo,
            HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var consulta = context.Set<Examen>().AsNoTracking()
                .Where(e => e.IdDocente == usuarioId)
                .Include(e => e.Materia)
                .AsQueryable();
            if (materiaId.HasValue) consulta = consulta.Where(e => e.IdMateria == materiaId.Value);
            if (cicloLectivo.HasValue) consulta = consulta.Where(e => e.CicloLectivo == cicloLectivo.Value);

            return Results.Ok(await consulta
                .OrderByDescending(e => e.Fecha)
                .Select(e => new
                {
                    e.IdExamen,
                    e.IdMateria,
                    Materia = e.Materia!.Nombre,
                    e.CicloLectivo,
                    e.Fecha,
                    e.TipoExamen,
                    e.NotaMinimaRegularizacion,
                    e.NotaMinimaPromocion,
                    CantidadNotas = e.Notas.Count,
                    CantidadInscriptos = context.InscripcionesExamenes.Count(i => i.ExamenId == e.IdExamen)
                })
                .ToListAsync());
        });

        docente.MapPut("/examenes/{examenId:int}/fecha", async (int examenId,
            ExamenFechaActualizarDto dto, HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var examen = await context.Set<Examen>().SingleOrDefaultAsync(e => e.IdExamen == examenId);
            if (examen is null) return Results.NotFound(new { mensaje = "La evaluación no existe." });
            if (examen.IdDocente != usuarioId) return Results.Forbid();
            if (dto.Fecha == default)
                return Results.BadRequest(new { mensaje = "Ingresá una fecha válida." });
            var fechaAnterior = examen.Fecha;
            examen.Fecha = dto.Fecha;
            await context.SaveChangesAsync();
            await NotificacionAutomaticaService.NotificarCambioFechaFinalAsync(
                examen.IdExamen, fechaAnterior, context);
            await NotificacionAutomaticaService.ReprogramarExamenAsync(examen.IdExamen, context);
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "La fecha se modificó correctamente." });
        });

        docente.MapDelete("/examenes/{examenId:int}", async (int examenId,
            HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var examen = await context.Set<Examen>().SingleOrDefaultAsync(e => e.IdExamen == examenId);
            if (examen is null) return Results.NotFound(new { mensaje = "La evaluación no existe." });
            if (examen.IdDocente != usuarioId) return Results.Forbid();
            var estudiantes = await context.EstudianteMaterias.AsNoTracking()
                .Where(i => i.IdMateria == examen.IdMateria && i.IdDocente == examen.IdDocente &&
                    i.CicloLectivo == examen.CicloLectivo && i.Estado != "Cancelada")
                .Select(i => i.IdEstudiante)
                .Distinct()
                .ToListAsync();
            await NotificacionAutomaticaService.EliminarRecordatoriosExamenAsync(examenId, context);
            context.Remove(examen);
            await context.SaveChangesAsync();
            foreach (var estudianteId in estudiantes)
                await ActualizarEstadoMateriaAsync(estudianteId, examen, context);
            await context.SaveChangesAsync();
            return Results.NoContent();
        });

        docente.MapPut("/examenes/{examenId:int}/criterios", async (int examenId,
            CriteriosEvaluacionDto dto, HttpContext http, AppDbContext context) =>
        {
            if (dto.NotaMinimaRegularizacion is < 0 or > 10)
                return Results.BadRequest(new { mensaje = "La nota para regularizar debe estar entre 0 y 10." });
            if (dto.NotaMinimaPromocion.HasValue &&
                (dto.NotaMinimaPromocion.Value is < 0 or > 10 ||
                 dto.NotaMinimaPromocion.Value < dto.NotaMinimaRegularizacion))
                return Results.BadRequest(new
                {
                    mensaje = "La nota para promocionar debe estar entre 0 y 10 y no puede ser menor que la nota para regularizar."
                });

            var usuarioId = http.User.ObtenerUsuarioId();
            var examen = await context.Set<Examen>().SingleOrDefaultAsync(e => e.IdExamen == examenId);
            if (examen is null) return Results.NotFound(new { mensaje = "La evaluación no existe." });
            if (examen.IdDocente != usuarioId) return Results.Forbid();
            examen.NotaMinimaRegularizacion = dto.NotaMinimaRegularizacion;
            examen.NotaMinimaPromocion = examen.TipoExamen.Equals("Final", StringComparison.OrdinalIgnoreCase)
                ? null
                : dto.NotaMinimaPromocion;
            var notasExistentes = await context.Set<NotaExamen>()
                .Where(n => n.IdExamen == examenId)
                .ToListAsync();
            foreach (var nota in notasExistentes)
            {
                nota.Condicion = EsFinal(examen) && nota.Nota >= examen.NotaMinimaRegularizacion
                    ? "Aprobó"
                    : examen.NotaMinimaPromocion.HasValue && nota.Nota >= examen.NotaMinimaPromocion.Value
                    ? "Promocionó"
                    : nota.Nota >= examen.NotaMinimaRegularizacion
                        ? "Regularizó"
                        : "Desaprobó";
            }
            await context.SaveChangesAsync();
            foreach (var estudianteId in notasExistentes.Select(n => n.IdEstudiante).Distinct())
                await ActualizarEstadoMateriaAsync(estudianteId, examen, context);
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Los criterios de calificación se guardaron correctamente." });
        });

        docente.MapGet("/examenes/{examenId:int}/planilla", async (int examenId,
            HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var examen = await context.Set<Examen>().AsNoTracking()
                .Include(e => e.Materia)
                .SingleOrDefaultAsync(e => e.IdExamen == examenId);
            if (examen is null) return Results.NotFound(new { mensaje = "La evaluación no existe." });
            if (examen.IdDocente != usuarioId) return Results.Forbid();

            var esFinal = examen.TipoExamen.Equals("Final", StringComparison.OrdinalIgnoreCase);
            var estudiantes = esFinal
                ? await context.InscripcionesExamenes.AsNoTracking()
                    .Where(i => i.ExamenId == examenId && i.Estado == "Inscripto")
                    .OrderBy(i => i.Estudiante.Persona.Apellido)
                    .ThenBy(i => i.Estudiante.Persona.Nombre)
                    .Select(i => new PlanillaEstudianteItem(
                        i.EstudianteId,
                        i.Estudiante.Persona.Apellido + ", " + i.Estudiante.Persona.Nombre,
                        i.Estudiante.Legajo))
                    .ToListAsync()
                : await context.EstudianteMaterias.AsNoTracking()
                    .Where(i => i.IdMateria == examen.IdMateria &&
                        i.CicloLectivo == examen.CicloLectivo && i.IdDocente == usuarioId &&
                        i.Estado != "Cancelada")
                    .OrderBy(i => i.Estudiante!.Persona.Apellido)
                    .ThenBy(i => i.Estudiante!.Persona.Nombre)
                    .Select(i => new PlanillaEstudianteItem(
                        i.IdEstudiante,
                        i.Estudiante!.Persona.Apellido + ", " + i.Estudiante.Persona.Nombre,
                        i.Estudiante.Legajo))
                    .ToListAsync();

            var notas = await context.Set<NotaExamen>().AsNoTracking()
                .Where(n => n.IdExamen == examenId)
                .ToDictionaryAsync(n => n.IdEstudiante);
            var ahora = DateTime.UtcNow;
            var fechaHabilitacion = examen.Fecha.Date.AddDays(1);
            var fechaLimite = examen.Fecha.Date.AddDays(15).AddTicks(-1);
            var habilitada = !esFinal || ahora >= fechaHabilitacion && ahora <= fechaLimite;
            var motivo = !esFinal
                ? null
                : ahora < fechaHabilitacion
                    ? "La carga de notas se habilitará cuando haya pasado la fecha del final."
                    : ahora > fechaLimite
                        ? "El plazo de 14 días para cargar las notas de este final ya finalizó."
                        : null;

            return Results.Ok(new
            {
                Examen = new
                {
                    examen.IdExamen,
                    examen.IdMateria,
                    Materia = examen.Materia!.Nombre,
                    examen.TipoExamen,
                    examen.Fecha,
                    examen.CicloLectivo,
                    examen.NotaMinimaRegularizacion,
                    examen.NotaMinimaPromocion,
                    EsFinal = esFinal,
                    CargaHabilitada = habilitada,
                    MotivoBloqueo = motivo,
                    FechaLimiteCarga = esFinal ? fechaLimite : (DateTime?)null
                },
                Estudiantes = estudiantes.Select(e =>
                {
                    notas.TryGetValue(e.IdEstudiante, out var nota);
                    return new
                    {
                        e.IdEstudiante,
                        e.Estudiante,
                        e.Legajo,
                        Nota = nota?.Nota,
                        Observaciones = nota?.Observaciones,
                        Condicion = nota?.Condicion
                    };
                })
            });
        });

        docente.MapGet("/examenes/{examenId:int}/notas", async (int examenId,
            HttpContext http, AppDbContext context) =>
        {
            var usuarioId = http.User.ObtenerUsuarioId();
            var examen = await context.Set<Examen>().AsNoTracking()
                .SingleOrDefaultAsync(e => e.IdExamen == examenId);
            if (examen is null) return Results.NotFound(new { mensaje = "La evaluación no existe." });
            if (examen.IdDocente != usuarioId) return Results.Forbid();

            return Results.Ok(await context.Set<NotaExamen>().AsNoTracking()
                .Where(n => n.IdExamen == examenId)
                .Include(n => n.Estudiante).ThenInclude(e => e!.Persona)
                .OrderBy(n => n.Estudiante!.Persona.Apellido)
                .ThenBy(n => n.Estudiante!.Persona.Nombre)
                .Select(n => new
                {
                    n.IdNota,
                    n.IdEstudiante,
                    Estudiante = n.Estudiante!.Persona.Apellido + ", " + n.Estudiante.Persona.Nombre,
                    n.Nota,
                    n.Observaciones
                })
                .ToListAsync());
        });

        docente.MapPost("/notas", async (NotaExamenCrearDto dto,
            HttpContext http, AppDbContext context) =>
        {
            if (dto.Nota is < 0 or > 10)
                return Results.BadRequest(new { mensaje = "La nota debe estar entre 0 y 10." });

            var usuarioId = http.User.ObtenerUsuarioId();
            var examen = await context.Set<Examen>().SingleOrDefaultAsync(e => e.IdExamen == dto.IdExamen);
            if (examen is null) return Results.NotFound(new { mensaje = "El examen no existe." });
            if (examen.IdDocente != usuarioId) return Results.Forbid();

            var inscripto = await context.EstudianteMaterias.AnyAsync(i =>
                i.IdEstudiante == dto.IdEstudiante && i.IdMateria == examen.IdMateria &&
                i.CicloLectivo == examen.CicloLectivo && i.IdDocente == usuarioId &&
                i.Estado != "Cancelada");
            if (!inscripto)
                return Results.BadRequest(new { mensaje = "El estudiante no pertenece a esta comisión." });
            if (examen.TipoExamen.Equals("Final", StringComparison.OrdinalIgnoreCase) &&
                !await context.InscripcionesExamenes.AnyAsync(i =>
                    i.ExamenId == examen.IdExamen && i.EstudianteId == dto.IdEstudiante))
                return Results.BadRequest(new { mensaje = "El estudiante no está inscripto en este examen final." });
            if (examen.TipoExamen.Equals("Final", StringComparison.OrdinalIgnoreCase))
            {
                var ahora = DateTime.UtcNow;
                var fechaHabilitacion = examen.Fecha.Date.AddDays(1);
                var fechaLimite = examen.Fecha.Date.AddDays(15).AddTicks(-1);
                if (ahora < fechaHabilitacion)
                    return Results.Conflict(new { mensaje = "Todavía no pasó la fecha del examen final." });
                if (ahora > fechaLimite)
                    return Results.Conflict(new { mensaje = "Finalizó el plazo de 14 días para cargar esta nota." });
            }

            var nota = await context.Set<NotaExamen>().SingleOrDefaultAsync(n =>
                n.IdExamen == dto.IdExamen && n.IdEstudiante == dto.IdEstudiante);
            if (nota is null)
            {
                nota = new NotaExamen { IdExamen = dto.IdExamen, IdEstudiante = dto.IdEstudiante };
                context.Add(nota);
            }
            nota.Nota = dto.Nota;
            nota.Observaciones = dto.Observaciones;
            nota.Condicion = EsFinal(examen) && dto.Nota >= examen.NotaMinimaRegularizacion
                ? "Aprobó"
                : examen.NotaMinimaPromocion.HasValue && dto.Nota >= examen.NotaMinimaPromocion.Value
                ? "Promocionó"
                : dto.Nota >= examen.NotaMinimaRegularizacion
                    ? "Regularizó"
                    : "Desaprobó";
            await context.SaveChangesAsync();
            await ActualizarEstadoMateriaAsync(dto.IdEstudiante, examen, context);
            await context.SaveChangesAsync();
            return Results.Ok(new { nota.IdNota, nota.Condicion });
        });
    }

    private static string ConstruirUrlPublica(HttpContext http, string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return string.Empty;
        if (Uri.TryCreate(relativeUrl, UriKind.Absolute, out _)) return relativeUrl;
        return $"{http.Request.Scheme}://{http.Request.Host}{relativeUrl}";
    }

    private sealed record PlanillaEstudianteItem(int IdEstudiante, string Estudiante, string Legajo);

    private static bool EsFinal(Examen examen) =>
        examen.TipoExamen.Equals("Final", StringComparison.OrdinalIgnoreCase);

    private static async Task ActualizarEstadoMateriaAsync(int estudianteId, Examen examen,
        AppDbContext context)
    {
        var inscripcion = await context.EstudianteMaterias.SingleOrDefaultAsync(i =>
            i.IdEstudiante == estudianteId && i.IdMateria == examen.IdMateria &&
            i.CicloLectivo == examen.CicloLectivo && i.IdDocente == examen.IdDocente &&
            i.Estado != "Cancelada");
        if (inscripcion is null) return;

        var finalAprobado = await context.Set<Examen>().AsNoTracking().AnyAsync(e =>
            e.IdMateria == examen.IdMateria && e.CicloLectivo == examen.CicloLectivo &&
            e.IdDocente == examen.IdDocente && e.TipoExamen == "Final" &&
            context.InscripcionesExamenes.Any(i => i.ExamenId == e.IdExamen &&
                i.EstudianteId == estudianteId && i.Estado == "Inscripto") &&
            context.Set<NotaExamen>().Any(n => n.IdExamen == e.IdExamen &&
                n.IdEstudiante == estudianteId && n.Nota >= e.NotaMinimaRegularizacion));
        if (finalAprobado)
        {
            inscripcion.Estado = "Aprobada";
            return;
        }

        if (inscripcion.Estado is "Aprobada" or "Promocionada")
            inscripcion.Estado = "En curso";

        var evaluaciones = await context.Set<Examen>().AsNoTracking()
            .Where(e => e.IdMateria == examen.IdMateria && e.CicloLectivo == examen.CicloLectivo &&
                e.IdDocente == examen.IdDocente && e.TipoExamen != "Final")
            .ToListAsync();
        if (evaluaciones.Count == 0) return;
        var ids = evaluaciones.Select(e => e.IdExamen).ToList();
        var notas = await context.Set<NotaExamen>().AsNoTracking()
            .Where(n => n.IdEstudiante == estudianteId && ids.Contains(n.IdExamen))
            .ToListAsync();
        if (notas.Count != evaluaciones.Count) return;

        if (notas.All(n => n.Condicion == "Promocionó"))
            inscripcion.Estado = "Promocionada";
        else if (evaluaciones.All(e => notas.Single(n => n.IdExamen == e.IdExamen).Nota >= e.NotaMinimaRegularizacion))
            inscripcion.Estado = "Regular";
        else
            inscripcion.Estado = "En curso";
    }

    private static async Task<bool> EstaHabilitadoFinancieramenteAsync(int estudianteId,
        AppDbContext context)
    {
        var matriculaAlDia = await context.MatriculasIniciales.AsNoTracking().AnyAsync(m =>
            m.EstudianteId == estudianteId &&
            (m.Estado == EstadoPago.AlDia || m.Estado == EstadoPago.Exentado));
        return matriculaAlDia && await EstaAlDiaConCuotasAsync(estudianteId, context);
    }

    private static async Task<bool> EstaAlDiaConCuotasAsync(int estudianteId, AppDbContext context)
    {
        var ahora = DateTime.UtcNow;
        var cuotasVencidas = context.CuotasMensuales.AsNoTracking().Where(c =>
            c.EstudianteId == estudianteId &&
            (c.Anio < ahora.Year || c.Anio == ahora.Year && c.Mes <= ahora.Month));
        var tieneCuotaActualHabilitada = await cuotasVencidas.AnyAsync(c =>
            c.Anio == ahora.Year && c.Mes == ahora.Month &&
            (c.Estado == EstadoPago.AlDia || c.Estado == EstadoPago.Exentado));
        if (!tieneCuotaActualHabilitada) return false;
        return !await cuotasVencidas.AnyAsync(c =>
            c.Estado != EstadoPago.AlDia && c.Estado != EstadoPago.Exentado);
    }

    private static DateTime ObtenerAhoraArgentina() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ObtenerZonaArgentina());

    // Las fechas de mesa se cargan como horario local argentino, sin zona horaria.
    private static DateTime ConvertirAFechaArgentina(DateTime fecha) =>
        fecha.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTimeFromUtc(fecha, ObtenerZonaArgentina())
            : DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified);

    private static TimeZoneInfo ObtenerZonaArgentina()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time"); }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
    }
}
