using System.ComponentModel.DataAnnotations;

public class ClaseAcademica
{
    public int Id { get; set; }
    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;
    public int? ComisionId { get; set; }
    public Comision? Comision { get; set; }
    public int DocenteId { get; set; }
    public Usuario Docente { get; set; } = null!;
    public DateTime Fecha { get; set; }

    [MaxLength(20)]
    public string Modalidad { get; set; } = "Presencial";

    [MaxLength(250)]
    public string AulaOEnlace { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Observaciones { get; set; } = string.Empty;

    public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
}
