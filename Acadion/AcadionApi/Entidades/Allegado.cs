using System.ComponentModel.DataAnnotations;

public class Allegado
{
    [Key]
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;

    [MaxLength(120)]
    public string NombreApellido { get; set; } = string.Empty;

    [MaxLength(60)]
    public string Relacion { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;
}
