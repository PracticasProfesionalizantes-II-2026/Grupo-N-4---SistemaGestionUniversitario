using AcadionApi.Datos;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class EquivalenciasEndpoints
{
    public static void MapEquivalenciasEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/equivalencias")
            .RequierePermiso(PermisosSistema.AcademicoGestionar);

        group.MapGet("/estudiante/{estudianteId:int}", async (int estudianteId, AppDbContext context) =>
            Results.Ok(await context.Set<EquivalenciaMateria>().AsNoTracking()
                .Where(e => e.EstudianteId == estudianteId)
                .OrderByDescending(e => e.FechaOtorgamientoUtc)
                .Select(e => new
                {
                    e.Id,
                    e.MateriaOrigenId,
                    MateriaOrigen = e.MateriaOrigen.Nombre,
                    CarreraOrigen = e.MateriaOrigen.AnioCursada!.Carrera!.Nombre,
                    e.MateriaDestinoId,
                    MateriaDestino = e.MateriaDestino.Nombre,
                    CarreraDestino = e.MateriaDestino.AnioCursada!.Carrera!.Nombre,
                    e.FechaOtorgamientoUtc,
                    e.Observaciones
                }).ToListAsync()));

        group.MapGet("/estudiante/{estudianteId:int}/candidatas", async (int estudianteId,
            AppDbContext context) =>
        {
            var estudiante = await context.Usuarios.AsNoTracking()
                .Where(u => u.Id == estudianteId && u.RolId == RolesSistema.EstudianteId)
                .Select(u => new
                {
                    u.Id,
                    Nombre = u.Persona.Apellido + ", " + u.Persona.Nombre,
                    u.Legajo,
                    u.CarreraId,
                    u.PlanEstudioId,
                    Carrera = u.Carrera == null ? "Sin carrera" : u.Carrera.Nombre,
                    Plan = u.PlanEstudio == null ? "Sin plan" : u.PlanEstudio.Codigo
                }).SingleOrDefaultAsync();
            if (estudiante is null)
                return Results.NotFound(new { mensaje = "El estudiante no existe." });

            var origenes = await context.EstudianteMaterias.AsNoTracking()
                .Where(i => i.IdEstudiante == estudianteId &&
                    new[] { "Aprobada", "Aprobado", "Promocionada", "Promocionado" }.Contains(i.Estado))
                .OrderBy(i => i.Materia!.Nombre)
                .Select(i => new
                {
                    Id = i.IdMateria,
                    Nombre = i.Materia!.Nombre,
                    Carrera = i.Materia.AnioCursada!.Carrera!.Nombre,
                    Plan = i.Materia.PlanEstudio == null ? "Plan histórico" : i.Materia.PlanEstudio.Codigo
                }).Distinct().ToListAsync();

            var destinosOtorgados = context.EquivalenciasMaterias
                .Where(e => e.EstudianteId == estudianteId).Select(e => e.MateriaDestinoId);
            var destinos = await context.Materias.AsNoTracking()
                .Where(m => m.AnioCursada!.IdCarrera == estudiante.CarreraId &&
                    (!estudiante.PlanEstudioId.HasValue || m.PlanEstudioId == estudiante.PlanEstudioId) &&
                    !destinosOtorgados.Contains(m.IdMateria))
                .OrderBy(m => m.AnioCursada!.NumeroAnio).ThenBy(m => m.Nombre)
                .Select(m => new { Id = m.IdMateria, m.Nombre, Anio = m.AnioCursada!.NumeroAnio })
                .ToListAsync();

            return Results.Ok(new { Estudiante = estudiante, MateriasOrigen = origenes, MateriasDestino = destinos });
        });

        group.MapPost("/", async (EquivalenciaCrearDto dto, HttpContext http, AppDbContext context) =>
        {
            var estudiante = await context.Usuarios.SingleOrDefaultAsync(u =>
                u.Id == dto.EstudianteId && u.RolId == RolesSistema.EstudianteId);
            if (estudiante is null) return Results.NotFound(new { mensaje = "El estudiante no existe." });
            var origen = await context.Materias.Include(m => m.AnioCursada)
                .SingleOrDefaultAsync(m => m.IdMateria == dto.MateriaOrigenId);
            var destino = await context.Materias.Include(m => m.AnioCursada)
                .SingleOrDefaultAsync(m => m.IdMateria == dto.MateriaDestinoId);
            if (origen is null || destino is null || origen.IdMateria == destino.IdMateria)
                return Results.BadRequest(new { mensaje = "Las materias de origen y destino no son válidas." });
            if (estudiante.CarreraId != destino.AnioCursada!.IdCarrera ||
                (estudiante.PlanEstudioId.HasValue && destino.PlanEstudioId != estudiante.PlanEstudioId))
                return Results.BadRequest(new { mensaje = "La materia de destino no pertenece al plan actual del estudiante." });
            var aprobada = await context.EstudianteMaterias.AnyAsync(i =>
                i.IdEstudiante == dto.EstudianteId && i.IdMateria == dto.MateriaOrigenId &&
                new[] { "Aprobada", "Aprobado", "Promocionada", "Promocionado" }.Contains(i.Estado));
            if (!aprobada)
                return Results.Conflict(new { mensaje = "La materia de origen no figura aprobada en el historial del estudiante." });
            if (await context.Set<EquivalenciaMateria>().AnyAsync(e =>
                e.EstudianteId == dto.EstudianteId && e.MateriaDestinoId == dto.MateriaDestinoId))
                return Results.Conflict(new { mensaje = "La equivalencia de destino ya fue otorgada." });

            await using var transaction = await context.Database.BeginTransactionAsync();
            context.Add(new EquivalenciaMateria
            {
                EstudianteId = dto.EstudianteId,
                MateriaOrigenId = dto.MateriaOrigenId,
                MateriaDestinoId = dto.MateriaDestinoId,
                OtorgadaPorUsuarioId = http.User.ObtenerUsuarioId(),
                Observaciones = dto.Observaciones?.Trim() ?? string.Empty
            });
            if (!await context.EstudianteMaterias.AnyAsync(i =>
                i.IdEstudiante == dto.EstudianteId && i.IdMateria == dto.MateriaDestinoId &&
                i.CicloLectivo == dto.CicloLectivo))
            {
                context.EstudianteMaterias.Add(new EstudianteMateria
                {
                    IdEstudiante = dto.EstudianteId,
                    IdMateria = dto.MateriaDestinoId,
                    CicloLectivo = dto.CicloLectivo,
                    Cuatrimestre = "Equivalencia",
                    FechaInscripcion = DateTime.UtcNow,
                    Estado = "Aprobada"
                });
            }
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Results.Ok(new { mensaje = "La equivalencia fue otorgada y agregada al historial académico." });
        });
    }
}

public class EquivalenciaCrearDto
{
    public int EstudianteId { get; set; }
    public int MateriaOrigenId { get; set; }
    public int MateriaDestinoId { get; set; }
    public int CicloLectivo { get; set; }
    public string Observaciones { get; set; } = string.Empty;
}
