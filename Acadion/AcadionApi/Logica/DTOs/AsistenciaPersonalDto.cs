namespace AcadionApi.DTOs;

public class AsistenciaPersonalCrearDto
{
    public int UsuarioId { get; set; }
    public int MateriaId { get; set; }
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Estado { get; set; } = "Presente";
    public string? Observaciones { get; set; }
}

public class AsistenciaPersonalDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public int? MateriaId { get; set; }
    public string Materia { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool Justificada { get; set; }
    public string? Observaciones { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
    public bool TieneJustificativo { get; set; }
    public string JustificadaPor { get; set; } = string.Empty;
    public DateTime? FechaJustificacionUtc { get; set; }
}
