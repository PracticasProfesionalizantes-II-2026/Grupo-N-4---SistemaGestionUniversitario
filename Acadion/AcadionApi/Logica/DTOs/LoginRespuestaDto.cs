namespace AcadionApi.Logica.DTOs;

public class LoginRespuestaDto
{
    public int PersonaId { get; set; }
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Permisos { get; set; } = Array.Empty<string>();
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEnUtc { get; set; }
    public bool DebeCambiarPassword { get; set; }
}

public class CambiarPasswordDto
{
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNueva { get; set; } = string.Empty;
}
