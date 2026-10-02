using System.ComponentModel.DataAnnotations;

public class EquivalenciaMateria
{
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;
    public int MateriaOrigenId { get; set; }
    public Materia MateriaOrigen { get; set; } = null!;
    public int MateriaDestinoId { get; set; }
    public Materia MateriaDestino { get; set; } = null!;
    public DateTime FechaOtorgamientoUtc { get; set; } = DateTime.UtcNow;
    public int OtorgadaPorUsuarioId { get; set; }
    public Usuario OtorgadaPor { get; set; } = null!;
    [MaxLength(500)]
    public string Observaciones { get; set; } = string.Empty;
}
