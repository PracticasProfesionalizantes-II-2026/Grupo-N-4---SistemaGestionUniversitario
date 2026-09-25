using System.ComponentModel.DataAnnotations;

public class PeriodoInscripcionMateria
{
    [Key]
    public int Id { get; set; }
    public int CarreraId { get; set; }
    public Carrera Carrera { get; set; } = null!;
    public int? MateriaId { get; set; }
    public Materia? Materia { get; set; }
    public int CicloLectivo { get; set; }
    public DateTime FechaInicioUtc { get; set; }
    public DateTime FechaFinUtc { get; set; }
    public int? ModificadoPorUsuarioId { get; set; }
    public Usuario? ModificadoPor { get; set; }
    public DateTime FechaModificacionUtc { get; set; } = DateTime.UtcNow;
}
