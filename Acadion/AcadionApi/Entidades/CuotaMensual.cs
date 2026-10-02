using System.ComponentModel.DataAnnotations;

public class CuotaMensual
{
    [Key]
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;
    public int Anio { get; set; }
    public int Mes { get; set; }
    public EstadoPago Estado { get; set; } = EstadoPago.Pendiente;
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaPago { get; set; }
    [MaxLength(500)]
    public string ComprobanteUrl { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    [MaxLength(40)]
    public string MetodoPago { get; set; } = string.Empty;
    public int? ValidadoPorUsuarioId { get; set; }
    public Usuario? ValidadoPor { get; set; }
    public DateTime? FechaValidacionUtc { get; set; }
    [MaxLength(500)]
    public string Observaciones { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
}
