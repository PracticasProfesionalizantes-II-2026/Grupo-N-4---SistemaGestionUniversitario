using AcadionApi.Datos;
using AcadionApi.DTOs;
using AcadionApi.Logica;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class ComisionEndpoints
{
    public static void MapComisionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/comisiones")
            .RequierePermiso(PermisosSistema.AcademicoLeer);

        group.MapGet("/", async (int? materiaId, int? cicloLectivo, AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            var query = context.Comisiones.AsNoTracking()
                .Include(c => c.Materia).ThenInclude(m => m.AnioCursada)!.ThenInclude(a => a!.Carrera)
                .Include(c => c.Docente).ThenInclude(d => d!.Persona)
                .Include(c => c.Horarios)
                .AsQueryable();
            if (materiaId.HasValue) query = query.Where(c => c.MateriaId == materiaId.Value);
            query = query.Where(c => c.CicloLectivo == ciclo);

            var items = await query.OrderBy(c => c.Materia.Nombre).ThenBy(c => c.Nombre)
                .Select(c => new
                {
                    c.Id,
                    c.MateriaId,
                    Materia = c.Materia.Nombre,
                    Carrera = c.Materia.AnioCursada!.Carrera!.Nombre,
                    c.Nombre,
                    c.Turno,
                    c.CicloLectivo,
                    c.Cupo,
                    c.DocenteId,
                    Docente = c.Docente == null ? "A confirmar" :
                        c.Docente.Persona.Apellido + ", " + c.Docente.Persona.Nombre,
                    c.Activa,
                    Inscriptos = c.Inscripciones.Count(i => i.Estado != "Cancelada"),
                    EnEspera = c.ListaEspera.Count(l => l.Estado == "EnEspera"),
                    Horarios = c.Horarios.OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio)
                        .Select(h => new { h.Id, h.DiaSemana, h.HoraInicio, h.HoraFin })
                }).ToListAsync();
            return Results.Ok(items);
        });

        group.MapPost("/", async (ComisionCrearDto dto, AppDbContext context) =>
        {
            var validation = await ValidarAsync(dto, null, context);
            if (validation is not null) return validation;

            var comision = CrearEntidad(dto);
            context.Comisiones.Add(comision);
            if (dto.DocenteId.HasValue && !await context.DocentesMaterias.AnyAsync(dm =>
                dm.IdDocente == dto.DocenteId.Value && dm.IdMateria == dto.MateriaId &&
                dm.CicloLectivo == dto.CicloLectivo && dm.Activa))
            {
                context.DocentesMaterias.Add(new DocenteMateria
                {
                    IdDocente = dto.DocenteId.Value,
                    IdMateria = dto.MateriaId,
                    CicloLectivo = dto.CicloLectivo,
                    Cuatrimestre = "Comisión"
                });
            }
            await context.SaveChangesAsync();
            return Results.Created($"/api/comisiones/{comision.Id}", new { comision.Id });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);

        group.MapPut("/{id:int}", async (int id, ComisionActualizarDto dto, AppDbContext context) =>
        {
            var comision = await context.Comisiones.Include(c => c.Horarios)
                .SingleOrDefaultAsync(c => c.Id == id);
            if (comision is null) return Results.NotFound();
            var validation = await ValidarAsync(dto, id, context);
            if (validation is not null) return validation;
            var inscriptos = await context.EstudianteMaterias.CountAsync(i =>
                i.ComisionId == id && i.Estado != "Cancelada");
            if (dto.Cupo < inscriptos)
                return Results.Conflict(new { mensaje = $"El cupo no puede ser menor a los {inscriptos} estudiantes inscriptos." });

            comision.MateriaId = dto.MateriaId;
            comision.Nombre = dto.Nombre.Trim();
            comision.Turno = dto.Turno.Trim();
            comision.CicloLectivo = dto.CicloLectivo;
            comision.Cupo = dto.Cupo;
            comision.DocenteId = dto.DocenteId;
            comision.Activa = dto.Activa;
            context.HorariosComisiones.RemoveRange(comision.Horarios);
            comision.Horarios = dto.Horarios.Select(h => new HorarioComision
            {
                DiaSemana = h.DiaSemana.Trim(),
                HoraInicio = h.HoraInicio,
                HoraFin = h.HoraFin
            }).ToList();
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Comisión actualizada correctamente." });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);

        group.MapDelete("/{id:int}", async (int id, AppDbContext context) =>
        {
            var comision = await context.Comisiones.SingleOrDefaultAsync(c => c.Id == id);
            if (comision is null) return Results.NotFound();
            comision.Activa = false;
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "La comisión quedó inactiva y se conservó su historial." });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);

        group.MapGet("/{id:int}/estudiantes", async (int id, AppDbContext context) =>
        {
            if (!await context.Comisiones.AnyAsync(c => c.Id == id)) return Results.NotFound();
            var estudiantes = await context.EstudianteMaterias.AsNoTracking()
                .Where(i => i.ComisionId == id && i.Estado != "Cancelada")
                .OrderBy(i => i.Estudiante!.Persona.Apellido).ThenBy(i => i.Estudiante!.Persona.Nombre)
                .Select(i => new
                {
                    i.IdEstudianteMateria,
                    i.IdEstudiante,
                    Apellido = i.Estudiante!.Persona.Apellido,
                    Nombre = i.Estudiante.Persona.Nombre,
                    i.Estudiante.Legajo,
                    i.Estado,
                    i.FechaInscripcion
                }).ToListAsync();
            return Results.Ok(estudiantes);
        });

        group.MapGet("/{id:int}/lista-espera", async (int id, AppDbContext context) =>
        {
            if (!await context.Comisiones.AnyAsync(c => c.Id == id)) return Results.NotFound();
            var lista = await context.ListasEsperaComisiones.AsNoTracking()
                .Where(l => l.ComisionId == id)
                .OrderBy(l => l.FechaSolicitudUtc)
                .Select((l) => new
                {
                    l.Id,
                    l.EstudianteId,
                    Apellido = l.Estudiante.Persona.Apellido,
                    Nombre = l.Estudiante.Persona.Nombre,
                    l.Estudiante.Legajo,
                    l.Estado,
                    l.FechaSolicitudUtc
                }).ToListAsync();
            return Results.Ok(lista);
        });

        group.MapPost("/{id:int}/lista-espera/{esperaId:int}/confirmar", async (
            int id, int esperaId, HttpContext http, AppDbContext context) =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            var espera = await context.ListasEsperaComisiones
                .Include(l => l.Comision).ThenInclude(c => c.Materia)
                .SingleOrDefaultAsync(l => l.Id == esperaId && l.ComisionId == id && l.Estado == "EnEspera");
            if (espera is null) return Results.NotFound(new { mensaje = "La solicitud ya no está pendiente." });
            var ocupados = await context.EstudianteMaterias.CountAsync(i =>
                i.ComisionId == id && i.Estado != "Cancelada");
            if (!PoliticasAcademicas.HayVacante(espera.Comision.Cupo, ocupados))
                return Results.Conflict(new { mensaje = "La comisión continúa sin vacantes disponibles." });
            if (await context.EstudianteMaterias.AnyAsync(i => i.IdEstudiante == espera.EstudianteId &&
                i.IdMateria == espera.Comision.MateriaId && i.CicloLectivo == espera.Comision.CicloLectivo))
                return Results.Conflict(new { mensaje = "El estudiante ya está inscripto en la materia." });

            context.EstudianteMaterias.Add(new EstudianteMateria
            {
                IdEstudiante = espera.EstudianteId,
                IdMateria = espera.Comision.MateriaId,
                ComisionId = espera.ComisionId,
                IdDocente = espera.Comision.DocenteId,
                CicloLectivo = espera.Comision.CicloLectivo,
                Cuatrimestre = CodigoPeriodo(espera.Comision.Materia),
                FechaInscripcion = DateTime.UtcNow,
                Estado = "Cursando"
            });
            espera.Estado = "Confirmada";
            espera.FechaResolucionUtc = DateTime.UtcNow;
            espera.ResueltoPorUsuarioId = http.User.ObtenerUsuarioId();
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Results.Ok(new { mensaje = "La vacante fue confirmada y el estudiante quedó inscripto." });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);
    }

    private static Comision CrearEntidad(ComisionCrearDto dto) => new()
    {
        MateriaId = dto.MateriaId,
        Nombre = dto.Nombre.Trim(),
        Turno = dto.Turno.Trim(),
        CicloLectivo = dto.CicloLectivo,
        Cupo = dto.Cupo,
        DocenteId = dto.DocenteId,
        Horarios = dto.Horarios.Select(h => new HorarioComision
        {
            DiaSemana = h.DiaSemana.Trim(),
            HoraInicio = h.HoraInicio,
            HoraFin = h.HoraFin
        }).ToList()
    };

    private static async Task<IResult?> ValidarAsync(
        ComisionCrearDto dto, int? excluirId, AppDbContext context)
    {
        if (dto.MateriaId <= 0 || !await context.Materias.AnyAsync(m => m.IdMateria == dto.MateriaId))
            return Results.BadRequest(new { mensaje = "La materia indicada no existe." });
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Turno))
            return Results.BadRequest(new { mensaje = "El nombre y el turno son obligatorios." });
        try
        {
            NormalizadorDatos.ValidarTextoAcademico(dto.Nombre, "El nombre de la comisión");
            NormalizadorDatos.ValidarTextoAcademico(dto.Turno, "El turno");
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { mensaje = ex.Message }); }
        if (dto.CicloLectivo is < 2020 or > 2100 || dto.Cupo < 1)
            return Results.BadRequest(new { mensaje = "El ciclo lectivo o el cupo no son válidos." });
        if (dto.Horarios.Count == 0)
            return Results.BadRequest(new { mensaje = "La comisión debe tener al menos un horario." });
        if (dto.Horarios.Any(h => h.HoraInicio >= h.HoraFin ||
            h.HoraFin - h.HoraInicio < TimeSpan.FromMinutes(40)))
            return Results.BadRequest(new { mensaje = "Cada horario debe durar al menos 40 minutos." });
        if (dto.Horarios.Any(actual => dto.Horarios.Any(otro => !ReferenceEquals(actual, otro) &&
            actual.DiaSemana == otro.DiaSemana && actual.HoraInicio < otro.HoraFin &&
            otro.HoraInicio < actual.HoraFin)))
            return Results.BadRequest(new { mensaje = "La comisión tiene horarios superpuestos." });
        if (dto.DocenteId.HasValue && !await context.Usuarios.AnyAsync(u =>
            u.Id == dto.DocenteId.Value && u.RolId == RolesSistema.DocenteId &&
            u.Estado == EstadoUsuario.Activo))
            return Results.BadRequest(new { mensaje = "El docente indicado no existe o no está activo." });
        if (await context.Comisiones.AnyAsync(c => c.Id != excluirId &&
            c.MateriaId == dto.MateriaId && c.CicloLectivo == dto.CicloLectivo &&
            c.Nombre == dto.Nombre.Trim()))
            return Results.Conflict(new { mensaje = "Ya existe una comisión con ese nombre para la materia y ciclo." });
        return null;
    }

    private static string CodigoPeriodo(Materia materia) => materia.TipoCursada switch
    {
        "Cuatrimestral" => $"{materia.NumeroPeriodo ?? 1}C",
        "Bimestral" => $"{materia.NumeroPeriodo ?? 1}B",
        _ => "Anual"
    };
}
