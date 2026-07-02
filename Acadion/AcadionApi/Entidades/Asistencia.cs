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

    // Datos de la Asistencia
    public DateTime Fecha { get; set; } = DateTime.Today; // Guarda la fecha del día actual
    
    // Tipo: "Presente", "Ausente" o "Justificada"
    public string Tipo { get; set; } = string.Empty; 
    
    public string? Observaciones { get; set; } // Por si hay que aclarar por qué se justificó la falta
}