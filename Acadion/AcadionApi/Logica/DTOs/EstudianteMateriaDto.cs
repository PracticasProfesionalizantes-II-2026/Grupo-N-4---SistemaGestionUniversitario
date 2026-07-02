using System;

namespace AcadionApi.DTOs
{
    public class InscripcionCrearDto
    {
        public int IdEstudiante { get; set; }
        public int IdMateria { get; set; }
        public int IdDocente { get; set; }
        public int CicloLectivo { get; set; }
        public string Cuatrimestre { get; set; } = "1C";
    }

    public class InscripcionDto
    {
        public int IdEstudianteMateria { get; set; }
        public int IdEstudiante { get; set; }
        public int IdMateria { get; set; }
        public int IdDocente { get; set; }
        public int CicloLectivo { get; set; }
        public string Cuatrimestre { get; set; } = string.Empty;
        public DateTime FechaInscripcion { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public class InscripcionActualizarDto
    {
        public int IdDocente { get; set; }
        public int CicloLectivo { get; set; }
        public string Cuatrimestre { get; set; } = "1C";
        public string Estado { get; set; } = "Cursando";
    }
}