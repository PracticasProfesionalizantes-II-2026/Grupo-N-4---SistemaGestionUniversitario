using System.ComponentModel.DataAnnotations;

public class NotificacionGeneral
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public int? RolDestinoId { get; set; }
    public Rol? RolDestino { get; set; }
    public int? UsuarioDestinoId { get; set; }
    public Usuario? UsuarioDestino { get; set; }
    public string Prioridad { get; set; } = "Normal";
    public DateTime FechaPublicacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FechaExpiracionUtc { get; set; }
    public bool Activa { get; set; } = true;
    public int? CreadaPorUsuarioId { get; set; }
    public Usuario? CreadaPor { get; set; }
    [MaxLength(180)]
    public string? ClaveAutomatica { get; set; }
}
