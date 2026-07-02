using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Logica;

namespace AcadionApi.Endpoints
{
    public static class ExamenEndpoints
    {
        public static void MapExamenEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/examenes");

            group.MapGet("/", async (IExamenLogica examenLogica) =>
            {
                try
                {
                    return Results.Ok(await examenLogica.ObtenerExamenesAsync());
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapGet("/{id:int}", async (int id, IExamenLogica examenLogica) =>
            {
                try
                {
                    var e = await examenLogica.ObtenerExamenPorIdAsync(id);
                    return e is not null ? Results.Ok(e) : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPost("/", async (ExamenCrearDto dto, IExamenLogica examenLogica) =>
            {
                try
                {
                    var resultado = await examenLogica.RegistrarExamenAsync(dto);
                    return Results.Created($"/examenes/{resultado.IdExamen}", resultado);
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

            group.MapPut("/{id:int}", async (int id, ExamenActualizarDto dto, IExamenLogica examenLogica) =>
            {
                try
                {
                    var actualizado = await examenLogica.ActualizarExamenAsync(id, dto);
                    return actualizado ? Results.Ok("Examen actualizado correctamente.") : Results.NotFound();
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

            group.MapDelete("/{id:int}", async (int id, IExamenLogica examenLogica) =>
            {
                try
                {
                    var eliminado = await examenLogica.EliminarExamenAsync(id);
                    return eliminado ? Results.Ok($"Examen con ID {id} eliminado.") : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });
        }
    }
}