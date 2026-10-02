public class RegistroAuditoria
{
    public long Id { get; set; }
    public int? UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Metodo { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public int EstadoHttp { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
}
