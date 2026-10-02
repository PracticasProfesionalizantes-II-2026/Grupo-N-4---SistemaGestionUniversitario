using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Logica;
using AcadionApi.Seguridad;

namespace AcadionApi.Endpoints
{
    public static class CarreraEndpoints
    {
        public static void MapCarreraEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/carreras")
                .RequierePermiso(PermisosSistema.AcademicoLeer);

            group.MapGet("/", async (ICarreraLogica carreraLogica) =>
            {
                try
                {
                    return Results.Ok(await carreraLogica.ObtenerCarrerasAsync());
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapGet("/{id:int}", async (int id, ICarreraLogica carreraLogica) =>
            {
                try
                {
                    var c = await carreraLogica.ObtenerCarreraPorIdAsync(id);
                    return c is not null ? Results.Ok(c) : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            group.MapPost("/", async (CarreraCrearDto dto, ICarreraLogica carreraLogica) =>
            {
                try
                {
                    var resultado = await carreraLogica.RegistrarCarreraAsync(dto);
                    return Results.Created($"/carreras/{resultado.IdCarrera}", resultado);
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
            }).RequierePermiso(PermisosSistema.AcademicoGestionar);

            group.MapPut("/{id:int}", async (int id, CarreraActualizarDto dto, ICarreraLogica carreraLogica) =>
            {
                try
                {
                    var actualizado = await carreraLogica.ActualizarCarreraAsync(id, dto);
                    return actualizado ? Results.Ok("Carrera actualizada correctamente.") : Results.NotFound();
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
            }).RequierePermiso(PermisosSistema.AcademicoGestionar);

        }
    }
}
