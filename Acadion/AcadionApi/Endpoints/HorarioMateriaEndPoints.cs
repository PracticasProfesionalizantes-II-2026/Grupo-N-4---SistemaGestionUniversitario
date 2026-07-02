using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Logica;

namespace AcadionApi.Endpoints
{
    public static class HorarioMateriaEndpoints
    {
        public static void MapHorarioMateriaEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/horarios");

            group.MapGet("/", async (IHorarioMateriaLogica horarioLogica) =>
            {
                try
                {
                    return Results.Ok(await horarioLogica.ObtenerHorariosAsync());
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapGet("/{id:int}", async (int id, IHorarioMateriaLogica horarioLogica) =>
            {
                try
                {
                    var h = await horarioLogica.ObtenerHorarioPorIdAsync(id);
                    return h is not null ? Results.Ok(h) : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPost("/", async (HorarioMateriaCrearDto dto, IHorarioMateriaLogica horarioLogica) =>
            {
                try
                {
                    var resultado = await horarioLogica.RegistrarHorarioAsync(dto);
                    return Results.Created($"/horarios/{resultado.IdHorarioMateria}", resultado);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPut("/{id:int}", async (int id, HorarioMateriaActualizarDto dto, IHorarioMateriaLogica horarioLogica) =>
            {
                try
                {
                    var actualizado = await horarioLogica.ActualizarHorarioAsync(id, dto);
                    return actualizado ? Results.Ok("Horario modificado correctamente.") : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapDelete("/{id:int}", async (int id, IHorarioMateriaLogica horarioLogica) =>
            {
                try
                {
                    var eliminado = await horarioLogica.EliminarHorarioAsync(id);
                    return eliminado ? Results.Ok($"Horario con ID {id} eliminado.") : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });
        }
    }
}