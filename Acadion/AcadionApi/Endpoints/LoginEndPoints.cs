using AcadionApi.DTOs;
using AcadionApi.Logica;
using AcadionApi.Logica.DTOs;
using AcadionApi.Seguridad;

namespace AcadionApi.Endpoints
{
    public static class LoginEndpoints
    {
        public static void MapLoginEndpoints(this WebApplication app)
        {
            app.MapPost("/api/auth/login",
                async (
                    LoginDto dto,
                    ILoginLogica LoginLogica) =>
                {
                    try
                    {
                        var loginResultado = await LoginLogica.LoginAsync(dto);

                        if (loginResultado is null)
                            return Results.Unauthorized();

                        return Results.Ok(loginResultado);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.Json(new { mensaje = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                    }
                });

            app.MapPost("/api/auth/cambiar-password",
                async (CambiarPasswordDto dto, HttpContext context, ILoginLogica loginLogica) =>
                {
                    try
                    {
                        var actualizado = await loginLogica.CambiarPasswordAsync(
                            context.User.ObtenerUsuarioId(), dto);

                        return actualizado
                            ? Results.NoContent()
                            : Results.BadRequest(new { mensaje = "La contraseña actual no es correcta." });
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new { mensaje = ex.Message });
                    }
                })
                .RequireAuthorization();
        }
    }
}
