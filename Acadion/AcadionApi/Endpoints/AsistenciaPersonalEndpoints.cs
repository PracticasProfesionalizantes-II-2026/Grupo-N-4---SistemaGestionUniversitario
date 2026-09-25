using AcadionApi.Datos;
using AcadionApi.DTOs;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class AsistenciaPersonalEndpoints
{
    public static void MapAsistenciaPersonalEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/asistencia-personal")
            .RequierePermiso(PermisosSistema.AsistenciaPersonalGestionar);

        group.MapGet("/", async (DateOnly? desde, DateOnly? hasta, AppDbContext context) =>
        {
            var consulta = context.RegistrosAsistenciaPersonal
                .AsNoTracking()
                .Include(r => r.Usuario).ThenInclude(u => u.Persona)
                .Include(r => r.Materia).ThenInclude(m => m!.AnioCursada).ThenInclude(a => a!.Carrera)
                .AsQueryable();

            if (desde.HasValue) consulta = consulta.Where(r => r.Fecha >= desde.Value);
            if (hasta.HasValue) consulta = consulta.Where(r => r.Fecha <= hasta.Value);

            var registros = await consulta
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();
            return Results.Ok(registros.Select(Mapear));
        });

        group.MapGet("/docentes", async (int? cicloLectivo, AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            if (ciclo is < 2020 or > 2100)
                return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });

            var docentes = await context.Usuarios.AsNoTracking()
                .Where(u => u.RolId == RolesSistema.DocenteId && u.Estado == EstadoUsuario.Activo)
                .Include(u => u.Persona)
                .OrderBy(u => u.Persona.Apellido)
                .ThenBy(u => u.Persona.Nombre)
                .Select(u => new
                {
                    u.Id,
                    u.Persona.Nombre,
                    u.Persona.Apellido,
                    u.Persona.Dni,
                    u.Especialidad
                })
                .ToListAsync();

            var asignaciones = await context.DocentesMaterias.AsNoTracking()
                .Where(dm => dm.CicloLectivo == ciclo && dm.Activa)
                .Include(dm => dm.Materia).ThenInclude(m => m.AnioCursada).ThenInclude(a => a!.Carrera)
                .OrderBy(dm => dm.Materia.AnioCursada!.Carrera!.Nombre)
                .ThenBy(dm => dm.Materia.AnioCursada!.NumeroAnio)
                .ThenBy(dm => dm.Materia.Nombre)
                .ToListAsync();

            return Results.Ok(docentes.Select(docente => new
            {
                docente.Id,
                docente.Nombre,
                docente.Apellido,
                docente.Dni,
                docente.Especialidad,
                Materias = asignaciones
                    .Where(dm => dm.IdDocente == docente.Id)
                    .Select(dm => new
                    {
                        MateriaId = dm.IdMateria,
                        Materia = dm.Materia.Nombre,
                        Carrera = dm.Materia.AnioCursada!.Carrera!.Nombre,
                        NumeroAnio = dm.Materia.AnioCursada.NumeroAnio,
                        dm.Cuatrimestre,
                        dm.CicloLectivo
                    })
            }));
        });

        group.MapPost("/", async (AsistenciaPersonalCrearDto dto,
            HttpContext http, AppDbContext context) =>
        {
            var usuario = await context.Usuarios
                .Include(u => u.Rol)
                .SingleOrDefaultAsync(u => u.Id == dto.UsuarioId);
            if (usuario is null)
                return Results.BadRequest(new { mensaje = "El usuario indicado no existe." });
            if (usuario.RolId != RolesSistema.DocenteId)
                return Results.BadRequest(new { mensaje = "El fichado de personal corresponde a docentes." });

            if (!EstadoValido(dto.Estado))
                return Results.BadRequest(new { mensaje = "El estado debe ser Presente o Ausente." });
            if (!await TieneAsignacionAsync(dto.UsuarioId, dto.MateriaId, dto.Fecha.Year, context))
                return Results.BadRequest(new { mensaje = "El profesor no está asignado a esa materia en el ciclo lectivo seleccionado." });
            if (await context.RegistrosAsistenciaPersonal.AnyAsync(r =>
                r.UsuarioId == dto.UsuarioId && r.MateriaId == dto.MateriaId && r.Fecha == dto.Fecha))
                return Results.Conflict(new { mensaje = "Ya existe una asistencia para ese profesor, materia y fecha." });

            var registro = new RegistroAsistenciaPersonal
            {
                UsuarioId = dto.UsuarioId,
                MateriaId = dto.MateriaId,
                Fecha = dto.Fecha,
                Estado = NormalizarEstado(dto.Estado),
                Observaciones = NormalizarObservaciones(dto.Observaciones),
                RegistradoPorUsuarioId = http.User.ObtenerUsuarioId()
            };
            context.RegistrosAsistenciaPersonal.Add(registro);
            await context.SaveChangesAsync();
            return Results.Created($"/api/asistencia-personal/{registro.Id}", new { registro.Id });
        });

        group.MapPut("/{id:int}", async (int id, AsistenciaPersonalCrearDto dto,
            HttpContext http, AppDbContext context) =>
        {
            var registro = await context.RegistrosAsistenciaPersonal.FindAsync(id);
            if (registro is null) return Results.NotFound();

            if (!EstadoValido(dto.Estado))
                return Results.BadRequest(new { mensaje = "El estado debe ser Presente o Ausente." });
            if (!await TieneAsignacionAsync(dto.UsuarioId, dto.MateriaId, dto.Fecha.Year, context))
                return Results.BadRequest(new { mensaje = "El profesor no está asignado a esa materia en el ciclo lectivo seleccionado." });
            if (await context.RegistrosAsistenciaPersonal.AnyAsync(r =>
                r.Id != id && r.UsuarioId == dto.UsuarioId &&
                r.MateriaId == dto.MateriaId && r.Fecha == dto.Fecha))
                return Results.Conflict(new { mensaje = "Ya existe una asistencia para ese profesor, materia y fecha." });

            registro.UsuarioId = dto.UsuarioId;
            registro.MateriaId = dto.MateriaId;
            registro.Fecha = dto.Fecha;
            registro.Estado = NormalizarEstado(dto.Estado);
            if (registro.Estado == "Presente") registro.Justificada = false;
            registro.Observaciones = NormalizarObservaciones(dto.Observaciones);
            registro.RegistradoPorUsuarioId = http.User.ObtenerUsuarioId();
            registro.FechaRegistroUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPut("/{id:int}/justificacion", async (int id,
            JustificacionInasistenciaDto dto, AppDbContext context) =>
        {
            var registro = await context.RegistrosAsistenciaPersonal.FindAsync(id);
            if (registro is null)
                return Results.NotFound(new { mensaje = "La asistencia docente indicada no existe." });
            if (!registro.Estado.Equals("Ausente", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { mensaje = "Solo se pueden justificar registros ausentes." });

            registro.Justificada = dto.Justificada;
            await context.SaveChangesAsync();
            return Results.Ok(new
            {
                registro.Id,
                registro.Justificada,
                mensaje = registro.Justificada
                    ? "La inasistencia docente fue justificada correctamente."
                    : "Se quitó la justificación de la inasistencia docente."
            });
        });
    }

    private static AsistenciaPersonalDto Mapear(RegistroAsistenciaPersonal r) => new()
    {
        Id = r.Id,
        UsuarioId = r.UsuarioId,
        NombreCompleto = $"{r.Usuario.Persona.Nombre} {r.Usuario.Persona.Apellido}",
        MateriaId = r.MateriaId,
        Materia = r.Materia?.Nombre ?? "Registro anterior",
        Carrera = r.Materia?.AnioCursada?.Carrera?.Nombre ?? string.Empty,
        Fecha = r.Fecha,
        Estado = r.Estado,
        Justificada = r.Justificada,
        Observaciones = r.Observaciones,
        RegistradoPorUsuarioId = r.RegistradoPorUsuarioId
    };

    private static bool EstadoValido(string? estado) =>
        string.Equals(estado?.Trim(), "Presente", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(estado?.Trim(), "Ausente", StringComparison.OrdinalIgnoreCase);

    private static string NormalizarEstado(string estado) =>
        string.Equals(estado.Trim(), "Presente", StringComparison.OrdinalIgnoreCase)
            ? "Presente"
            : "Ausente";

    private static string? NormalizarObservaciones(string? observaciones) =>
        string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();

    private static Task<bool> TieneAsignacionAsync(int docenteId, int materiaId,
        int cicloLectivo, AppDbContext context) =>
        context.DocentesMaterias.AsNoTracking().AnyAsync(dm =>
            dm.IdDocente == docenteId && dm.IdMateria == materiaId &&
            dm.CicloLectivo == cicloLectivo && dm.Activa);
}
