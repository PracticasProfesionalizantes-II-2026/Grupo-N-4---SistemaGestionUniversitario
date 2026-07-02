using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Materia
{
    [Key]
    public int IdMateria { get; set; }
    public string Nombre { get; set; } = string.Empty;
    
    // Modalidad: "Presencial", "Virtual", "Mixta" (según tu diagrama)
    public string Modalidad { get; set; } = string.Empty; 
    
    // EstadoMateria: "Activa", "Inactiva" (por si cambia el plan de estudios)
    public string Estado { get; set; } = string.Empty;
    
    // Una materia tiene una lista de horarios asignados
    public List<HorarioMateria> Horarios { get; set; } = new List<HorarioMateria>();
    
    // Relación con el año académico al que pertenece esta materia
    public int IdAnio { get; set; }
    public Anio? AnioCursada { get; set; }

    // Relación de Correlatividades 
    // Una materia puede tener una lista de otras materias como correlativas
    public List<Materia> Correlativas { get; set; } = new List<Materia>();

    // Método de validación
    // Verifica si un estudiante cumple con las materias correlativas aprobadas
    public bool ValidarCorrelativas(Usuario estudiante)
    {
        // Aca va ña lógica para comparar las materias aprobadas del estudiante
        // contra la lista 'Correlativas' de esta materia.
        return true; 
    }
}