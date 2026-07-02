using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Logica;

namespace AcadionApi.Endpoints
{
    public static class AsistenciaEndpoints
    {
        public static void MapAsistenciaEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/asistencias");

            group.MapGet("/", async (IAsistenciaLogica asistenciaLogica) =>
            {
                try
                {
                    return Results.Ok(await asistenciaLogica.ObtenerAsistenciasAsync());
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapGet("/{id:int}", async (int id, IAsistenciaLogica asistenciaLogica) =>
            {
                try
                {
                    var a = await asistenciaLogica.ObtenerAsistenciaPorIdAsync(id);
                    return a is not null ? Results.Ok(a) : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPost("/", async (AsistenciaCrearDto dto, IAsistenciaLogica asistenciaLogica) =>
            {
                try
                {
                    var resultado = await asistenciaLogica.RegistrarAsistenciaAsync(dto);
                    return Results.Created($"/asistencias/{resultado.IdAsistencia}", resultado);
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

            group.MapPut("/{id:int}", async (int id, AsistenciaActualizarDto dto, IAsistenciaLogica asistenciaLogica) =>
            {
                try
                {
                    var actualizado = await asistenciaLogica.ActualizarAsistenciaAsync(id, dto);
                    return actualizado ? Results.Ok("Asistencia actualizada correctamente.") : Results.NotFound();
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

            group.MapDelete("/{id:int}", async (int id, IAsistenciaLogica asistenciaLogica) =>
            {
                try
                {
                    var eliminado = await asistenciaLogica.EliminarAsistenciaAsync(id);
                    return eliminado ? Results.Ok($"Asistencia con ID {id} eliminada.") : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });
        }
    }
}