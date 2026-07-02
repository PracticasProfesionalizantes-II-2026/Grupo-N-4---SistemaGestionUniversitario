using System;

namespace AcadionApi.DTOs
{
    public class ExamenCrearDto
    {
        public int IdMateria { get; set; }
        public int CicloLectivo { get; set; }
        public int IdDocente { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoExamen { get; set; } = "Parcial";
    }

    public class ExamenDto
    {
        public int IdExamen { get; set; }
        public int IdMateria { get; set; }
        public int CicloLectivo { get; set; }
        public int IdDocente { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoExamen { get; set; } = string.Empty;
    }

    public class ExamenActualizarDto
    {
        public int IdMateria { get; set; }
        public int CicloLectivo { get; set; }
        public int IdDocente { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoExamen { get; set; } = "Parcial";
    }
}