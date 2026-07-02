using System;

namespace AcadionApi.DTOs
{
    public class AsistenciaCrearDto
    {
        public int IdEstudianteMateria { get; set; }
        public int IdDocente { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;
        public string Tipo { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }

    public class AsistenciaDto
    {
        public int IdAsistencia { get; set; }
        public int IdEstudianteMateria { get; set; }
        public int IdDocente { get; set; }
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }

    public class AsistenciaActualizarDto
    {
        public int IdDocente { get; set; }
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}