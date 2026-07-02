using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Logica;

namespace AcadionApi.Endpoints
{
    public static class UsuarioEndpoints
    {
        // Método de extensión para registrar todas las rutas de usuarios juntas
        public static void MapUsuarioEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/usuarios");

            // GET /usuarios
            group.MapGet("/", async (IUsuarioLogica usuarioLogica) =>
            {
                try
                {
                    var usuarios = await usuarioLogica.ObtenerUsuariosAsync();
                    return Results.Ok(usuarios);
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            // GET /usuarios/{id}
            group.MapGet("/{id:int}", async (int id, IUsuarioLogica usuarioLogica) =>
            {
                try
                {
                    var usuario = await usuarioLogica.ObtenerUsuarioPorIdAsync(id);
                    return usuario is not null ? Results.Ok(usuario) : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });

            // POST /usuarios
            group.MapPost("/", async (UsuarioCrearDto dto, IUsuarioLogica usuarioLogica) =>
            {
                try
                {
                    var resultado = await usuarioLogica.RegistrarUsuarioAsync(dto);
                    return Results.Created($"/usuarios/{resultado.Id}", resultado);
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

            // PUT /usuarios/{id}
            group.MapPut("/{id:int}", async (int id, UsuarioActualizarDto dto, IUsuarioLogica usuarioLogica) =>
            {
                try
                {
                    var actualizado = await usuarioLogica.ActualizarUsuarioAsync(id, dto);
                    return actualizado ? Results.Ok("Usuario actualizado correctamente.") : Results.NotFound();
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

            // DELETE /usuarios/{id}
            group.MapDelete("/{id:int}", async (int id, IUsuarioLogica usuarioLogica) =>
            {
                try
                {
                    var eliminado = await usuarioLogica.EliminarUsuarioAsync(id);
                    return eliminado ? Results.Ok($"Usuario con ID {id} eliminado.") : Results.NotFound();
                }
                catch (Exception)
                {
                    return Results.StatusCode(500);
                }
            });
        }
    }
}