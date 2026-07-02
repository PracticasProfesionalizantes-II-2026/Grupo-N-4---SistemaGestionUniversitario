using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class EstudianteMateria
{
    [Key]
    public int IdEstudianteMateria { get; set; }

    // Quién se inscribe (Apuntamos al Id heredado de Persona)
    public int IdEstudiante { get; set; }
    public Usuario? Estudiante { get; set; }

    // Se inscribe a una materia específica
    public int IdMateria { get; set; }
    public Materia? Materia { get; set; }

    // Qué docente dicta esta cursada este año
    public int IdDocente { get; set; }
    public Usuario? Docente { get; set; }

    // Contexto temporal 
    public int CicloLectivo { get; set; } // Ej: 2026
    public string Cuatrimestre { get; set; } = "1C"; // "1C", "2C", "Anual"

    public DateTime FechaInscripcion { get; set; } = DateTime.Now;
    public string Estado { get; set; } = "Cursando"; // "Cursando", "Regular", "Libre", "Aprobada"

    public bool ValidarCorrelativas(Usuario estudiante)
    {
        return true; 
    }
}