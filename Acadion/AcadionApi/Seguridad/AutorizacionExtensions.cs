using System.Security.Claims;

namespace AcadionApi.Seguridad;

public static class AutorizacionExtensions
{
    public static TBuilder RequierePermiso<TBuilder>(this TBuilder builder, string permiso)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(policy => policy.RequireClaim(ClaimsSistema.Permiso, permiso));
        return builder;
    }

    public static int ObtenerUsuarioId(this ClaimsPrincipal principal)
    {
        var valor = principal.FindFirstValue(ClaimsSistema.UsuarioId);
        return int.TryParse(valor, out var id)
            ? id
            : throw new UnauthorizedAccessException("El token no contiene un usuario válido.");
    }

    public static bool TienePermiso(this ClaimsPrincipal principal, string permiso) =>
        principal.Claims.Any(c => c.Type == ClaimsSistema.Permiso && c.Value == permiso);
}
