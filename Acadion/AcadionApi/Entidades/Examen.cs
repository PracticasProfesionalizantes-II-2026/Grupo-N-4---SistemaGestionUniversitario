using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Examen
{
    [Key]
    public int IdExamen { get; set; }

    // CORRECCIÓN: El examen pertenece a una MATERIA específica
    public int IdMateria { get; set; }
    public Materia? Materia { get; set; }

    // Cuándo se rinde el examen (ej: Ciclo 2026, 1er Cuatrimestre)
    public int CicloLectivo { get; set; } 

    public int IdDocente { get; set; }
    public Usuario? Docente { get; set; }

    public DateTime Fecha { get; set; }
    public string TipoExamen { get; set; } = "Parcial"; // "Parcial", "Final", "Recuperatorio"

    public List<NotaExamen> Notas { get; set; } = new List<NotaExamen>();
}