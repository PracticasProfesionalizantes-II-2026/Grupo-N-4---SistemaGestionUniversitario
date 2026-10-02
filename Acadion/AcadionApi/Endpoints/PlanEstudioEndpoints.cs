using AcadionApi.Datos;
using AcadionApi.DTOs;
using AcadionApi.Logica;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class PlanEstudioEndpoints
{
    public static void MapPlanEstudioEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/planes-estudio")
            .RequierePermiso(PermisosSistema.AcademicoLeer);

        group.MapGet("/carrera/{carreraId:int}", async (int carreraId, AppDbContext context) =>
        {
            if (!await context.Set<Carrera>().AnyAsync(c => c.IdCarrera == carreraId))
                return Results.NotFound(new { mensaje = "La carrera indicada no existe." });

            var planes = await context.PlanesEstudio.AsNoTracking()
                .Where(p => p.CarreraId == carreraId)
                .OrderByDescending(p => p.Activo)
                .ThenByDescending(p => p.VigenteDesde)
                .Select(p => new PlanEstudioDto
                {
                    Id = p.Id,
                    CarreraId = p.CarreraId,
                    Codigo = p.Codigo,
                    VigenteDesde = p.VigenteDesde,
                    VigenteHasta = p.VigenteHasta,
                    Activo = p.Activo,
                    CantidadMaterias = p.Materias.Count,
                    CantidadEstudiantes = p.Estudiantes.Count
                }).ToListAsync();
            return Results.Ok(planes);
        });

        group.MapPost("/carrera/{carreraId:int}", async (int carreraId,
            PlanEstudioCrearDto dto, AppDbContext context) =>
        {
            var error = Validar(dto.Codigo, dto.VigenteDesde, null);
            if (error is not null) return Results.BadRequest(new { mensaje = error });

            var carrera = await context.Set<Carrera>()
                .Include(c => c.PlanesEstudio)
                .SingleOrDefaultAsync(c => c.IdCarrera == carreraId);
            if (carrera is null)
                return Results.NotFound(new { mensaje = "La carrera indicada no existe." });

            var codigo = dto.Codigo.Trim();
            if (carrera.PlanesEstudio.Any(p => p.Codigo == codigo))
                return Results.Conflict(new { mensaje = "Ya existe un plan con ese código en la carrera." });

            await using var transaccion = await context.Database.BeginTransactionAsync();
            var anterior = carrera.PlanesEstudio.SingleOrDefault(p => p.Activo);
            if (anterior is not null)
            {
                anterior.Activo = false;
                anterior.VigenteHasta ??= Math.Max(anterior.VigenteDesde, dto.VigenteDesde - 1);
            }

            var nuevo = new PlanEstudio
            {
                CarreraId = carreraId,
                Codigo = codigo,
                VigenteDesde = dto.VigenteDesde,
                Activo = true
            };
            context.PlanesEstudio.Add(nuevo);
            carrera.PlanEstudios = codigo;
            await context.SaveChangesAsync();

            if (dto.CopiarMateriasDelPlanVigente && anterior is not null)
                await CopiarMateriasAsync(anterior.Id, nuevo.Id, context);

            await context.SaveChangesAsync();
            await transaccion.CommitAsync();
            return Results.Created($"/api/planes-estudio/{nuevo.Id}", new
            {
                nuevo.Id,
                nuevo.CarreraId,
                nuevo.Codigo,
                nuevo.VigenteDesde,
                nuevo.Activo
            });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);

        group.MapPut("/{id:int}", async (int id, PlanEstudioActualizarDto dto,
            AppDbContext context) =>
        {
            var error = Validar(dto.Codigo, dto.VigenteDesde, dto.VigenteHasta);
            if (error is not null) return Results.BadRequest(new { mensaje = error });

            var plan = await context.PlanesEstudio.Include(p => p.Carrera)
                .SingleOrDefaultAsync(p => p.Id == id);
            if (plan is null) return Results.NotFound();
            var codigo = dto.Codigo.Trim();
            if (await context.PlanesEstudio.AnyAsync(p => p.Id != id &&
                p.CarreraId == plan.CarreraId && p.Codigo == codigo))
                return Results.Conflict(new { mensaje = "Ya existe un plan con ese código en la carrera." });

            if (dto.Activo)
            {
                var otros = await context.PlanesEstudio
                    .Where(p => p.CarreraId == plan.CarreraId && p.Id != id && p.Activo)
                    .ToListAsync();
                foreach (var otro in otros)
                {
                    otro.Activo = false;
                    otro.VigenteHasta ??= Math.Max(otro.VigenteDesde, dto.VigenteDesde - 1);
                }
                plan.Carrera.PlanEstudios = codigo;
            }

            plan.Codigo = codigo;
            plan.VigenteDesde = dto.VigenteDesde;
            plan.VigenteHasta = dto.Activo ? null : dto.VigenteHasta;
            plan.Activo = dto.Activo;
            await context.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Plan de estudios actualizado correctamente." });
        }).RequierePermiso(PermisosSistema.AcademicoGestionar);
    }

    private static string? Validar(string codigo, int desde, int? hasta)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return "El código del plan es obligatorio.";
        if (codigo.Trim().Length > 80) return "El código del plan no puede superar 80 caracteres.";
        try { NormalizadorDatos.ValidarTextoAcademico(codigo, "El código del plan"); }
        catch (ArgumentException ex) { return ex.Message; }
        if (desde is < 2000 or > 2100) return "La vigencia inicial debe ser un año válido.";
        if (hasta.HasValue && hasta.Value < desde)
            return "La vigencia final no puede ser anterior a la inicial.";
        return null;
    }

    private static async Task CopiarMateriasAsync(int origenId, int destinoId, AppDbContext context)
    {
        var originales = await context.Materias
            .Where(m => m.PlanEstudioId == origenId)
            .Include(m => m.Horarios)
            .Include(m => m.Correlativas)
            .OrderBy(m => m.AnioCursada!.NumeroAnio)
            .ToListAsync();
        var copias = originales.ToDictionary(m => m.IdMateria, m => new Materia
        {
            Nombre = m.Nombre,
            Modalidad = m.Modalidad,
            Estado = m.Estado,
            TipoCursada = m.TipoCursada,
            NumeroPeriodo = m.NumeroPeriodo,
            IdAnio = m.IdAnio,
            PlanEstudioId = destinoId,
            Horarios = m.Horarios.Select(h => new HorarioMateria
            {
                DiaSemana = h.DiaSemana,
                HoraInicio = h.HoraInicio,
                HoraFin = h.HoraFin
            }).ToList()
        });
        foreach (var original in originales)
        {
            copias[original.IdMateria].Correlativas = original.Correlativas
                .Where(c => copias.ContainsKey(c.IdMateria))
                .Select(c => copias[c.IdMateria])
                .ToList();
        }
        context.Materias.AddRange(copias.Values);
    }
}
