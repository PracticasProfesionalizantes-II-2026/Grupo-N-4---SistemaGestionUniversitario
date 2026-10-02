using System.ComponentModel.DataAnnotations;

public class TurnoExamenFinal
{
    public int Id { get; set; }
    public int CicloLectivo { get; set; }
    [MaxLength(80)] public string Nombre { get; set; } = string.Empty;
    public int NumeroLlamado { get; set; } = 1;
    public DateTime FechaInicioUtc { get; set; }
    public DateTime FechaFinUtc { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Examen> Examenes { get; set; } = new List<Examen>();
}
