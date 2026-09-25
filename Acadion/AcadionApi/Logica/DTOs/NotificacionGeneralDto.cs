namespace AcadionApi.DTOs;

public class NotificacionGeneralCrearDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public int? RolDestinoId { get; set; }
    public string Prioridad { get; set; } = "Normal";
    public DateTime? FechaExpiracionUtc { get; set; }
}

public class NotificacionGeneralDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public int? RolDestinoId { get; set; }
    public string Destinatarios { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public DateTime FechaPublicacionUtc { get; set; }
    public DateTime? FechaExpiracionUtc { get; set; }
    public bool Activa { get; set; }
    public string CreadaPor { get; set; } = string.Empty;
}
