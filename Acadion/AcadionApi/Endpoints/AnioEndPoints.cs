using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Logica;

namespace AcadionApi.Endpoints
{
    public static class AnioEndpoints
    {
        public static void MapAnioEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/anios");

            group.MapGet("/", async (IAnioLogica anioLogica) =>
            {
                try
                {
                    return Results.Ok(await anioLogica.ObtenerAniosAsync());
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapGet("/{id:int}", async (int id, IAnioLogica anioLogica) =>
            {
                try
                {
                    var a = await anioLogica.ObtenerAnioPorIdAsync(id);
                    return a is not null ? Results.Ok(a) : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPost("/", async (AnioCrearDto dto, IAnioLogica anioLogica) =>
            {
                try
                {
                    var resultado = await anioLogica.RegistrarAnioAsync(dto);
                    return Results.Created($"/anios/{resultado.IdAnio}", resultado);
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

            group.MapPut("/{id:int}", async (int id, AnioActualizarDto dto, IAnioLogica anioLogica) =>
            {
                try
                {
                    var actualizado = await anioLogica.ActualizarAnioAsync(id, dto);
                    return actualizado ? Results.Ok("Año académico actualizado correctamente.") : Results.NotFound();
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

            group.MapDelete("/{id:int}", async (int id, IAnioLogica anioLogica) =>
            {
                try
                {
                    var eliminado = await anioLogica.EliminarAnioAsync(id);
                    return eliminado ? Results.Ok($"Año académico con ID {id} eliminado.") : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });
        }
    }
}