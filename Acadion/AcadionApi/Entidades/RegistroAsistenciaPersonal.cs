using System.ComponentModel.DataAnnotations;

public class RegistroAsistenciaPersonal
{
    [Key]
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int? MateriaId { get; set; }
    public Materia? Materia { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly? HoraEntrada { get; set; }
    public TimeOnly? HoraSalida { get; set; }
    public string Estado { get; set; } = "Presente";
    public bool Justificada { get; set; }
    public string? Observaciones { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
    public Usuario RegistradoPor { get; set; } = null!;
    public DateTime FechaRegistroUtc { get; set; } = DateTime.UtcNow;
}
