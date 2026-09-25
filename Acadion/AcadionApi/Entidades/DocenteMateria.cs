using System.ComponentModel.DataAnnotations;

public class DocenteMateria
{
    [Key]
    public int Id { get; set; }
    public int IdDocente { get; set; }
    public Usuario Docente { get; set; } = null!;
    public int IdMateria { get; set; }
    public Materia Materia { get; set; } = null!;
    public int CicloLectivo { get; set; }
    public string Cuatrimestre { get; set; } = "1C";
    public DateTime FechaAsignacionUtc { get; set; } = DateTime.UtcNow;
    public bool Activa { get; set; } = true;
}
