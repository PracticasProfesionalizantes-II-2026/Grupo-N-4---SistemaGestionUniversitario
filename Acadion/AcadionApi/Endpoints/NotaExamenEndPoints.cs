using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Logica;

namespace AcadionApi.Endpoints
{
    public static class NotaExamenEndpoints
    {
        public static void MapNotaExamenEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/notas");

            group.MapGet("/", async (INotaExamenLogica notaLogica) =>
            {
                try
                {
                    return Results.Ok(await notaLogica.ObtenerNotasAsync());
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapGet("/{id:int}", async (int id, INotaExamenLogica notaLogica) =>
            {
                try
                {
                    var n = await notaLogica.ObtenerNotaPorIdAsync(id);
                    return n is not null ? Results.Ok(n) : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPost("/", async (NotaExamenCrearDto dto, INotaExamenLogica notaLogica) =>
            {
                try
                {
                    var resultado = await notaLogica.RegistrarNotaAsync(dto);
                    return Results.Created($"/notas/{resultado.IdNota}", resultado);
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

            group.MapPut("/{id:int}", async (int id, NotaExamenActualizarDto dto, INotaExamenLogica notaLogica) =>
            {
                try
                {
                    var actualizado = await notaLogica.ActualizarNotaAsync(id, dto);
                    return actualizado ? Results.Ok("Nota actualizada correctamente.") : Results.NotFound();
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

            group.MapDelete("/{id:int}", async (int id, INotaExamenLogica notaLogica) =>
            {
                try
                {
                    var eliminado = await notaLogica.EliminarNotaAsync(id);
                    return eliminado ? Results.Ok($"Nota con ID {id} eliminada.") : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });
        }
    }
}