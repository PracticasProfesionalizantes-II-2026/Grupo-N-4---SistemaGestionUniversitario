namespace AcadionApi.DTOs;

public class AnioConMateriasCrearDto
{
    public int CarreraId { get; set; }
    public int NumeroAnio { get; set; }
    public string NombreAnio { get; set; } = string.Empty;
    public List<MateriaInicialDto> Materias { get; set; } = new();
}

public class MateriaInicialDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Modalidad { get; set; } = "Presencial";
    public List<HorarioInicialDto> Horarios { get; set; } = new();
}

public class HorarioInicialDto
{
    public string DiaSemana { get; set; } = string.Empty;
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
}

public class DocenteMateriaCrearDto
{
    public int DocenteId { get; set; }
    public int MateriaId { get; set; }
    public int CicloLectivo { get; set; }
    public string Cuatrimestre { get; set; } = "1C";
}

public class PeriodoInscripcionActualizarDto
{
    public int CarreraId { get; set; }
    public int MateriaId { get; set; }
    public int CicloLectivo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public class PeriodoInscripcionCarreraActualizarDto
{
    public int CarreraId { get; set; }
    public int CicloLectivo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public class PeriodoInscripcionExamenActualizarDto
{
    public int CicloLectivo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public class ComisionCrearDto
{
    public int MateriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Turno { get; set; } = string.Empty;
    public int CicloLectivo { get; set; }
    public int Cupo { get; set; }
    public int? DocenteId { get; set; }
    public List<HorarioInicialDto> Horarios { get; set; } = new();
}

public class ComisionActualizarDto : ComisionCrearDto
{
    public bool Activa { get; set; } = true;
}

public class ClaseAcademicaCrearDto
{
    public int MateriaId { get; set; }
    public int? ComisionId { get; set; }
    public DateTime Fecha { get; set; }
    public string Modalidad { get; set; } = "Presencial";
    public string AulaOEnlace { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}

public class EstadoExamenAlumnoDto
{
    public string Estado { get; set; } = string.Empty;
}
