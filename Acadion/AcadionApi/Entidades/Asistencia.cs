using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Asistencia
{
    [Key]
    public int IdAsistencia { get; set; }

    // Relación con la Inscripción del Alumno
    // En lugar de apuntar a 'Estudiante' y 'Materia' por separado,
    // apuntamos a 'EstudianteMateria'. Esto garantiza que el alumno realmente cursa la materia.
    public int IdEstudianteMateria { get; set; }
    public EstudianteMateria? Inscripcion { get; set; }

    // Relación con el Docente que tomó la asistencia
    // (Por si un profesor suplente toma la clase ese día)
    public int IdDocente { get; set; }
    public Usuario? Docente { get; set; } 

    public int? ClaseId { get; set; }
    public ClaseAcademica? Clase { get; set; }

    // Datos de la Asistencia
    public DateTime Fecha { get; set; } = DateTime.Today; // Guarda la fecha del día actual
    
    // Estado registrado por el docente: "Presente", "Ausente" o "Tardanza".
    public string Tipo { get; set; } = string.Empty; 
    public string TipoClase { get; set; } = "Presencial";
    public string TemaDictado { get; set; } = string.Empty;
    public bool Justificada { get; set; }
    [MaxLength(500)]
    public string JustificativoArchivo { get; set; } = string.Empty;
    public int? JustificadaPorUsuarioId { get; set; }
    public Usuario? JustificadaPor { get; set; }
    public DateTime? FechaJustificacionUtc { get; set; }
    public int CantidadInasistencias { get; set; }
    public string? Observaciones { get; set; }
}
