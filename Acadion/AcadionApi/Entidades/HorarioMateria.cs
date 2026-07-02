using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class HorarioMateria
{
    [Key]
    public int IdHorarioMateria { get; set; }
    public int IdMateria { get; set; } // A qué materia pertenece este horario
    
    public string DiaSemana { get; set; } = string.Empty; // "Lunes", "Miércoles"
    
    // Usamos TimeSpan para manejar horas en C# (ej: 16:00:00)
    public TimeSpan HoraInicio { get; set; } 
    public TimeSpan HoraFin { get; set; }
}