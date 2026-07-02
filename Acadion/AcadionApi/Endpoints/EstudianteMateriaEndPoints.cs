using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Logica;

namespace AcadionApi.Endpoints
{
    public static class EstudianteMateriaEndpoints
    {
        public static void MapEstudianteMateriaEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/inscripciones");

            group.MapGet("/", async (IEstudianteMateriaLogica logica) =>
            {
                try
                {
                    return Results.Ok(await logica.ObtenerInscripcionesAsync());
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapGet("/{id:int}", async (int id, IEstudianteMateriaLogica logica) =>
            {
                try
                {
                    var i = await logica.ObtenerInscripcionPorIdAsync(id);
                    return i is not null ? Results.Ok(i) : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPost("/", async (InscripcionCrearDto dto, IEstudianteMateriaLogica logica) =>
            {
                try
                {
                    var resultado = await logica.InscribirEstudianteAsync(dto);
                    return Results.Created($"/inscripciones/{resultado.IdEstudianteMateria}", resultado);
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

            group.MapPut("/{id:int}", async (int id, InscripcionActualizarDto dto, IEstudianteMateriaLogica logica) =>
            {
                try
                {
                    var actualizado = await logica.ActualizarInscripcionAsync(id, dto);
                    return actualizado ? Results.Ok("Inscripción actualizada correctamente.") : Results.NotFound();
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

            group.MapDelete("/{id:int}", async (int id, IEstudianteMateriaLogica logica) =>
            {
                try
                {
                    var eliminado = await logica.CancelarInscripcionAsync(id);
                    return eliminado ? Results.Ok($"Inscripción con ID {id} cancelada.") : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });
        }
    }
}