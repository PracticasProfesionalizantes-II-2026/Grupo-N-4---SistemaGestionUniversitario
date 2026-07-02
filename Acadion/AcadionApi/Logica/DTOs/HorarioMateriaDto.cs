using System;

namespace AcadionApi.DTOs
{
    public class HorarioMateriaCrearDto
    {
        public int IdMateria { get; set; }
        public string DiaSemana { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
    }

    public class HorarioMateriaDto
    {
        public int IdHorarioMateria { get; set; }
        public int IdMateria { get; set; }
        public string DiaSemana { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
    }

    public class HorarioMateriaActualizarDto
    {
        public int IdMateria { get; set; }
        public string DiaSemana { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
    }
}