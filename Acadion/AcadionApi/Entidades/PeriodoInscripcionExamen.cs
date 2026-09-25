using System.ComponentModel.DataAnnotations;

public class PeriodoInscripcionExamen
{
    [Key]
    public int Id { get; set; }
    public int CicloLectivo { get; set; }
    public DateTime FechaInicioUtc { get; set; }
    public DateTime FechaFinUtc { get; set; }
    public int ModificadoPorUsuarioId { get; set; }
    public Usuario ModificadoPor { get; set; } = null!;
    public DateTime FechaModificacionUtc { get; set; } = DateTime.UtcNow;
}
