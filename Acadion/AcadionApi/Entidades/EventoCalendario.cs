using System.ComponentModel.DataAnnotations;

public class EventoCalendario
{
    public int Id { get; set; }

    [MaxLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Tipo { get; set; } = "Institucional";

    [MaxLength(1000)]
    public string Descripcion { get; set; } = string.Empty;

    public DateTime FechaInicioUtc { get; set; }
    public DateTime FechaFinUtc { get; set; }
    public int? CarreraId { get; set; }
    public Carrera? Carrera { get; set; }
    public bool Activo { get; set; } = true;
    public int CreadoPorUsuarioId { get; set; }
    public Usuario CreadoPor { get; set; } = null!;
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
}
