using AcadionApi.Seguridad;

public class SeguridadContrasenaTests
{
    [Theory]
    [InlineData("corta1A")]
    [InlineData("sinmayusculas123")]
    [InlineData("SINMINUSCULAS123")]
    [InlineData("SinNumerosSeguros")]
    public void RechazaContrasenasDebiles(string password) =>
        Assert.Throws<ArgumentException>(() => SeguridadContrasena.Validar(password));

    [Fact]
    public void AceptaContrasenaRobusta() =>
        SeguridadContrasena.Validar("Acadion2026Seguro");
}
