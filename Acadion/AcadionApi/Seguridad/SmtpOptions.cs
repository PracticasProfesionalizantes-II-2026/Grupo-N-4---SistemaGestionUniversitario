namespace AcadionApi.Seguridad;

public sealed class SmtpOptions
{
    public const string Seccion = "Smtp";
    public string Host { get; set; } = string.Empty;
    public int Puerto { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Remitente { get; set; } = string.Empty;
    public string NombreRemitente { get; set; } = "Acadion";
}
