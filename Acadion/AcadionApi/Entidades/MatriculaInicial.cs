using System.ComponentModel.DataAnnotations;

public class MatriculaInicial
{
    [Key]
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;
    public int PeriodoLectivo { get; set; }
    public EstadoPago Estado { get; set; } = EstadoPago.Pendiente;
    public DateTime? FechaPago { get; set; }
    [MaxLength(500)]
    public string ComprobanteUrl { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
}
