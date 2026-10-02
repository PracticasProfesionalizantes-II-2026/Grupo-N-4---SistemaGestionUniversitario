using System.ComponentModel.DataAnnotations;

public class Comision
{
    public int Id { get; set; }
    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;

    [MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Turno { get; set; } = string.Empty;

    public int CicloLectivo { get; set; }
    public int Cupo { get; set; }
    public int? DocenteId { get; set; }
    public Usuario? Docente { get; set; }
    public bool Activa { get; set; } = true;

    public ICollection<HorarioComision> Horarios { get; set; } = new List<HorarioComision>();
    public ICollection<EstudianteMateria> Inscripciones { get; set; } = new List<EstudianteMateria>();
    public ICollection<ListaEsperaComision> ListaEspera { get; set; } = new List<ListaEsperaComision>();
}

public class HorarioComision
{
    public int Id { get; set; }
    public int ComisionId { get; set; }
    public Comision Comision { get; set; } = null!;

    [MaxLength(20)]
    public string DiaSemana { get; set; } = string.Empty;
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
}

public class ListaEsperaComision
{
    public int Id { get; set; }
    public int ComisionId { get; set; }
    public Comision Comision { get; set; } = null!;
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;
    public DateTime FechaSolicitudUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string Estado { get; set; } = "EnEspera";
    public DateTime? FechaResolucionUtc { get; set; }
    public int? ResueltoPorUsuarioId { get; set; }
    public Usuario? ResueltoPor { get; set; }
}
