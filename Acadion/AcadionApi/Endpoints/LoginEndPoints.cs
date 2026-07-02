using AcadionApi.DTOs;
using AcadionApi.Logica;

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
                    var loginCorrecto =
                        await LoginLogica.LoginAsync(dto);

                    if (!loginCorrecto)
                    {
                        return Results.Unauthorized();
                    }

                    return Results.Ok("Login exitoso");
                });
        }
    }
}