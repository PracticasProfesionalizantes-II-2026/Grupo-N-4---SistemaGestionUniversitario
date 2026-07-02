using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Logica;

namespace AcadionApi.Endpoints
{
    public static class PersonaEndpoints
    {
        public static void MapPersonaEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/personas");

            group.MapGet("/", async (IPersonaLogica personaLogica) =>
            {
                try
                {
                    return Results.Ok(await personaLogica.ObtenerPersonasAsync());
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapGet("/{id:int}", async (int id, IPersonaLogica personaLogica) =>
            {
                try
                {
                    var p = await personaLogica.ObtenerPersonaPorIdAsync(id);
                    return p is not null ? Results.Ok(p) : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPost("/", async (PersonaCrearDto dto, IPersonaLogica personaLogica) =>
            {
                try
                {
                    var resultado = await personaLogica.RegistrarPersonaAsync(dto);
                    return Results.Created($"/personas/{resultado.Id}", resultado);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(ex.Message);
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPut("/{id:int}", async (int id, PersonaActualizarDto dto, IPersonaLogica personaLogica) =>
            {
                try
                {
                    var actualizado = await personaLogica.ActualizarPersonaAsync(id, dto);
                    return actualizado ? Results.Ok("Persona actualizada correctamente.") : Results.NotFound();
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

            group.MapDelete("/{id:int}", async (int id, IPersonaLogica personaLogica) =>
            {
                try
                {
                    var eliminado = await personaLogica.EliminarPersonaAsync(id);
                    return eliminado ? Results.Ok($"Persona con ID {id} eliminada.") : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });
        }
    }
}