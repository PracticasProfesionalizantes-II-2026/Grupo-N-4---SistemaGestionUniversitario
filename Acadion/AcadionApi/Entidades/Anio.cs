using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Anio
{
    [Key]
    public int IdAnio { get; set; }
    
    // Representa el nivel: 1 para Primer Año, 2 para Segundo Año, etc.
    public int NumeroAnio { get; set; } 
    
    // Nombre descriptivo opcional (ej: "Primer Año", "Segundo Año")
    public string NombreAnio { get; set; } = string.Empty;

    // Relación con la Carrera a la que pertenece este año académico
    public int IdCarrera { get; set; }
    public Carrera? Carrera { get; set; }

    // Las materias que pertenecen estrictamente a este año de cursada
    public List<Materia> Materias { get; set; } = new List<Materia>();
}

