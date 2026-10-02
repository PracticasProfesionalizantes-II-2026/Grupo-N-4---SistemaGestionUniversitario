public class NotificacionLectura
{
    public int NotificacionId { get; set; }
    public NotificacionGeneral Notificacion { get; set; } = null!;
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime FechaLecturaUtc { get; set; } = DateTime.UtcNow;
}
