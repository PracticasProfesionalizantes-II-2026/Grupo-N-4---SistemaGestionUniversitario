using System.Security.Claims;
using AcadionApi.Seguridad;

public class PermisosTests
{
    [Fact]
    public void TienePermiso_ExigeElClaimExacto()
    {
        var identidad = new ClaimsIdentity(new[]
        {
            new Claim(ClaimsSistema.Permiso, PermisosSistema.ReportesLeer)
        }, "prueba");
        var usuario = new ClaimsPrincipal(identidad);

        Assert.True(usuario.TienePermiso(PermisosSistema.ReportesLeer));
        Assert.False(usuario.TienePermiso(PermisosSistema.UsuariosGestionar));
    }

    [Fact]
    public void ObtenerUsuarioId_RechazaUnaIdentidadSinIdValido()
    {
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "prueba"));

        Assert.Throws<UnauthorizedAccessException>(() => usuario.ObtenerUsuarioId());
    }
}
