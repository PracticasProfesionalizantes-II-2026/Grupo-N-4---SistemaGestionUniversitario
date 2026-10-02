using AcadionApi.DTOs;
using AcadionApi.Datos;
using AcadionApi.Logica;
using AcadionApi.Logica.DTOs;
using AcadionApi.Seguridad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AcadionApi.Endpoints
{
    public static class LoginEndpoints
    {
        public static void MapLoginEndpoints(this WebApplication app)
        {
            app.MapPost("/api/auth/login",
                async (
                    LoginDto dto,
                    ILoginLogica LoginLogica,
                    HttpContext http) =>
                {
                    try
                    {
                        var loginResultado = await LoginLogica.LoginAsync(dto);

                        if (loginResultado is null)
                            return Results.Unauthorized();

                        http.Response.Cookies.Append("acadion_access", loginResultado.Token,
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = !app.Environment.IsDevelopment(),
                                SameSite = SameSiteMode.Strict,
                                Expires = new DateTimeOffset(loginResultado.ExpiraEnUtc),
                                IsEssential = true,
                                Path = "/"
                            });
                        loginResultado.Token = string.Empty;
                        return Results.Ok(loginResultado);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.Json(new { mensaje = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                    }
                })
                .RequireRateLimiting("login");

            app.MapGet("/api/auth/recuperacion/disponible", (IEmailService emailService) =>
                Results.Ok(new { disponible = emailService.EstaConfigurado }));

            app.MapPost("/api/auth/recuperacion/solicitar",
                async (SolicitarRecuperacionDto dto, AppDbContext context,
                    IEmailService emailService, ILoggerFactory loggerFactory) =>
                {
                    if (!emailService.EstaConfigurado)
                        return Results.Json(new
                        {
                            mensaje = "La recuperación por correo todavía no está configurada. Contactá a Secretaría."
                        }, statusCode: StatusCodes.Status503ServiceUnavailable);

                    var email = dto.Email.Trim().ToLowerInvariant();
                    if (email.Length is < 5 or > 254 || !email.Contains('@'))
                        return Results.BadRequest(new { mensaje = "Ingresá un correo electrónico válido." });

                    var usuario = await context.Usuarios
                        .Include(u => u.Persona)
                        .SingleOrDefaultAsync(u =>
                            u.EmailInstitucional == email || u.Persona.Email == email);

                    const string respuestaGenerica =
                        "Si el correo corresponde a una cuenta activa, recibirás un código en los próximos minutos.";
                    if (usuario is null || usuario.Estado != EstadoUsuario.Activo)
                        return Results.Accepted(value: new { mensaje = respuestaGenerica });

                    var ahora = DateTime.UtcNow;
                    var anteriores = await context.RecuperacionesContrasena
                        .Where(r => r.UsuarioId == usuario.Id && r.FechaUsoUtc == null &&
                            r.FechaExpiracionUtc > ahora)
                        .ToListAsync();
                    anteriores.ForEach(r => r.FechaExpiracionUtc = ahora);

                    var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
                    var recuperacion = new RecuperacionContrasena
                    {
                        UsuarioId = usuario.Id,
                        FechaCreacionUtc = ahora,
                        FechaExpiracionUtc = ahora.AddMinutes(10)
                    };
                    recuperacion.CodigoHash = new PasswordHasher<RecuperacionContrasena>()
                        .HashPassword(recuperacion, codigo);
                    context.RecuperacionesContrasena.Add(recuperacion);
                    await context.SaveChangesAsync();

                    try
                    {
                        await emailService.EnviarAsync(email, "Código para recuperar tu cuenta de Acadion",
                            $"Tu código de recuperación es: {codigo}\n\n" +
                            "Vence en 10 minutos y solo puede utilizarse una vez. " +
                            "Si no solicitaste este cambio, ignorá este mensaje.");
                    }
                    catch (Exception error)
                    {
                        recuperacion.FechaExpiracionUtc = ahora;
                        await context.SaveChangesAsync();
                        loggerFactory.CreateLogger("RecuperacionContrasena")
                            .LogError(error, "No fue posible enviar el correo de recuperación.");
                        return Results.Json(new
                        {
                            mensaje = "No fue posible enviar el correo en este momento. Intentá nuevamente más tarde."
                        }, statusCode: StatusCodes.Status503ServiceUnavailable);
                    }

                    return Results.Accepted(value: new { mensaje = respuestaGenerica });
                })
                .RequireRateLimiting("recuperacion");

            app.MapPost("/api/auth/recuperacion/verificar",
                async (VerificarCodigoRecuperacionDto dto, AppDbContext context) =>
                {
                    var email = dto.Email.Trim().ToLowerInvariant();
                    var codigo = dto.Codigo.Trim();
                    if (codigo.Length != 6 || !codigo.All(char.IsDigit))
                        return Results.BadRequest(new { mensaje = "El código debe tener seis números." });

                    var ahora = DateTime.UtcNow;
                    var recuperacion = await context.RecuperacionesContrasena
                        .Include(r => r.Usuario).ThenInclude(u => u.Persona)
                        .Where(r => r.FechaUsoUtc == null && r.FechaExpiracionUtc > ahora &&
                            (r.Usuario.EmailInstitucional == email || r.Usuario.Persona.Email == email))
                        .OrderByDescending(r => r.FechaCreacionUtc)
                        .FirstOrDefaultAsync();
                    if (recuperacion is null || recuperacion.Intentos >= 5)
                        return Results.BadRequest(new { mensaje = "El código venció o no es válido. Solicitá uno nuevo." });

                    var resultado = new PasswordHasher<RecuperacionContrasena>()
                        .VerifyHashedPassword(recuperacion, recuperacion.CodigoHash, codigo);
                    if (resultado == PasswordVerificationResult.Failed)
                    {
                        recuperacion.Intentos++;
                        if (recuperacion.Intentos >= 5) recuperacion.FechaExpiracionUtc = ahora;
                        await context.SaveChangesAsync();
                        return Results.BadRequest(new { mensaje = "El código venció o no es válido." });
                    }

                    var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                    recuperacion.TokenHash = HashToken(token);
                    recuperacion.FechaVerificacionUtc = ahora;
                    recuperacion.FechaExpiracionTokenUtc = ahora.AddMinutes(15);
                    await context.SaveChangesAsync();
                    return Results.Ok(new { token, venceEnUtc = recuperacion.FechaExpiracionTokenUtc });
                })
                .RequireRateLimiting("recuperacion");

            app.MapPost("/api/auth/recuperacion/restablecer",
                async (RestablecerContrasenaDto dto, AppDbContext context) =>
                {
                    try
                    {
                        SeguridadContrasena.Validar(dto.PasswordNueva);
                    }
                    catch (ArgumentException error)
                    {
                        return Results.BadRequest(new { mensaje = error.Message });
                    }

                    var ahora = DateTime.UtcNow;
                    var tokenHash = HashToken(dto.Token.Trim());
                    var recuperacion = await context.RecuperacionesContrasena
                        .Include(r => r.Usuario)
                        .SingleOrDefaultAsync(r => r.TokenHash == tokenHash &&
                            r.FechaUsoUtc == null && r.FechaExpiracionTokenUtc > ahora);
                    if (recuperacion is null)
                        return Results.BadRequest(new { mensaje = "El enlace de recuperación venció. Solicitá un código nuevo." });

                    recuperacion.Usuario.PasswordHash = new PasswordHasher<Usuario>()
                        .HashPassword(recuperacion.Usuario, dto.PasswordNueva);
                    recuperacion.Usuario.DebeCambiarPassword = false;
                    recuperacion.FechaUsoUtc = ahora;
                    await context.SaveChangesAsync();
                    return Results.NoContent();
                })
                .RequireRateLimiting("recuperacion");

            app.MapPost("/api/auth/logout", (HttpContext http) =>
            {
                http.Response.Cookies.Delete("acadion_access", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !app.Environment.IsDevelopment(),
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
                return Results.NoContent();
            });

            app.MapGet("/api/auth/session", (HttpContext http) =>
                Results.Ok(new
                {
                    usuarioId = http.User.ObtenerUsuarioId(),
                    autenticada = true
                }))
                .RequireAuthorization();

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

        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
