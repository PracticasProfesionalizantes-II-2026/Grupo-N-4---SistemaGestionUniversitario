namespace AcadionApi.Logica.DTOs;

public sealed class SolicitarRecuperacionDto
{
    public string Email { get; set; } = string.Empty;
}

public sealed class VerificarCodigoRecuperacionDto
{
    public string Email { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}

public sealed class RestablecerContrasenaDto
{
    public string Token { get; set; } = string.Empty;
    public string PasswordNueva { get; set; } = string.Empty;
}
