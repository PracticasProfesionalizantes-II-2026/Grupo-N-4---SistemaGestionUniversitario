using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Logica;

namespace AcadionApi.Endpoints
{
    public static class MateriaEndpoints
    {
        public static void MapMateriaEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/materias");

            group.MapGet("/", async (IMateriaLogica materiaLogica) =>
            {
                try
                {
                    return Results.Ok(await materiaLogica.ObtenerMateriasAsync());
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapGet("/{id:int}", async (int id, IMateriaLogica materiaLogica) =>
            {
                try
                {
                    var m = await materiaLogica.ObtenerMateriaPorIdAsync(id);
                    return m is not null ? Results.Ok(m) : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPost("/", async (MateriaCrearDto dto, IMateriaLogica materiaLogica) =>
            {
                try
                {
                    var resultado = await materiaLogica.RegistrarMateriaAsync(dto);
                    return Results.Created($"/materias/{resultado.IdMateria}", resultado);
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

            group.MapPut("/{id:int}", async (int id, MateriaActualizarDto dto, IMateriaLogica materiaLogica) =>
            {
                try
                {
                    var actualizado = await materiaLogica.ActualizarMateriaAsync(id, dto);
                    return actualizado ? Results.Ok("Materia actualizada correctamente.") : Results.NotFound();
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

            group.MapDelete("/{id:int}", async (int id, IMateriaLogica materiaLogica) =>
            {
                try
                {
                    var eliminado = await materiaLogica.EliminarMateriaAsync(id);
                    return eliminado ? Results.Ok($"Materia con ID {id} eliminada.") : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });
        }
    }
}