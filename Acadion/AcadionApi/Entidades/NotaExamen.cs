using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
public class NotaExamen
{
    // Identificador único de la nota
    [Key]
    public int IdNota { get; set; }

    // Relación con el Examen (¿Qué examen rindió?)
    public int IdExamen { get; set; }
    public Examen? Examen { get; set; } // Propiedad de navegación al evento del examen

    // Relación con el Estudiante
    // Nota: Apuntamos al Usuario (con rol Estudiante) para saber de quién es la nota.
    public int IdEstudiante { get; set; }
    public Usuario? Estudiante { get; set; } // Propiedad de navegación al alumno

    // Datos de la calificación según tu diagrama
    public decimal Nota { get; set; }       // Ejemplo: Nota final (7.5, 8.0, 10)

    // Opcional: Podrías agregar observaciones por si el alumno dejó el examen incompleto, etc.
    public string? Observaciones { get; set; } 
}