using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
    
public class Carrera
{
    [Key]
    public int IdCarrera { get; set; }

    // Nombre de la carrera
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    // Identificación del plan de estudios asociado (ej: "Plan 2026")
    [MaxLength(80)]
    public string PlanEstudios { get; set; } = string.Empty;

    // Tipo de título que otorga la carrera.
    [MaxLength(30)]
    public string Tipo { get; set; } = string.Empty;

    // Cantidad de niveles anuales que componen el plan de estudios.
    public int DuracionAnios { get; set; }

    // Cupo total de estudiantes que pueden estar asignados a la carrera.
    public int CapacidadMaximaEstudiantes { get; set; }

    // Las carreras se conservan para no perder su historial académico.
    public bool Activa { get; set; } = true;

    // 4. Relación estructural: Una carrera se divide en varios años académicos (1°, 2°, 3°...)
    // De esta lista se desprenden luego las materias correspondientes a cada nivel.
    public List<Anio> AniosAcademicos { get; set; } = new List<Anio>();

    // 5. Lista de alumnos inscritos globalmente en esta carrera
    // Útil para métricas institucionales (saber cuántos alumnos activos tiene la carrera)
    public List<Usuario> AlumnosInscritos { get; set; } = new List<Usuario>();

    // Versiones históricas del plan. PlanEstudios se conserva como nombre del
    // plan vigente para mantener compatibilidad con las pantallas existentes.
    public List<PlanEstudio> PlanesEstudio { get; set; } = new List<PlanEstudio>();
}
