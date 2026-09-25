namespace AcadionApi.Seguridad;

public class JwtOptions
{
    public const string Seccion = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int MinutosExpiracion { get; set; } = 120;
}
