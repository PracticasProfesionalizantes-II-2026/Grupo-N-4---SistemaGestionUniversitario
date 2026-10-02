public class RecuperacionContrasena
{
    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string CodigoHash { get; set; } = string.Empty;
    public string? TokenHash { get; set; }
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime FechaExpiracionUtc { get; set; }
    public DateTime? FechaVerificacionUtc { get; set; }
    public DateTime? FechaExpiracionTokenUtc { get; set; }
    public DateTime? FechaUsoUtc { get; set; }
    public int Intentos { get; set; }
}
