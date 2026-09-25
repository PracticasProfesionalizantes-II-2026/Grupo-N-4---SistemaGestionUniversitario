using System.ComponentModel.DataAnnotations;

public class InscripcionExamen
{
    [Key]
    public int Id { get; set; }
    public int ExamenId { get; set; }
    public Examen Examen { get; set; } = null!;
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;
    public DateTime FechaInscripcionUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(30)]
    public string Estado { get; set; } = "Inscripto";
}
