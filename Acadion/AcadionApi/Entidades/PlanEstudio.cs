using System.ComponentModel.DataAnnotations;

public class PlanEstudio
{
    public int Id { get; set; }

    public int CarreraId { get; set; }
    public Carrera Carrera { get; set; } = null!;

    [MaxLength(80)]
    public string Codigo { get; set; } = string.Empty;

    public int VigenteDesde { get; set; }
    public int? VigenteHasta { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Materia> Materias { get; set; } = new List<Materia>();
    public ICollection<Usuario> Estudiantes { get; set; } = new List<Usuario>();
}
