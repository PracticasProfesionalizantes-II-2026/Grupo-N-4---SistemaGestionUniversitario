using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AcadionApi.Seguridad;

public record TokenGenerado(string Token, DateTime ExpiraEnUtc);

public interface ITokenService
{
    TokenGenerado Generar(Usuario usuario);
}

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public TokenGenerado Generar(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddMinutes(_options.MinutosExpiracion);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimsSistema.UsuarioId, usuario.Id.ToString()),
            new(ClaimsSistema.PersonaId, usuario.PersonaId.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Role, usuario.Rol.Nombre)
        };

        claims.AddRange(usuario.Rol.RolPermisos
            .Select(rp => new Claim(ClaimsSistema.Permiso, rp.Permiso.Codigo)));

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return new TokenGenerado(new JwtSecurityTokenHandler().WriteToken(jwt), expira);
    }
}
