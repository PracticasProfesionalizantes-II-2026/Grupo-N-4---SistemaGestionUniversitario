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
        public int? ExamenRecuperadoId { get; set; }
        public int? TurnoExamenFinalId { get; set; }
    }

    public class ExamenDto
    {
        public int IdExamen { get; set; }
        public int IdMateria { get; set; }
        public int CicloLectivo { get; set; }
        public int IdDocente { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoExamen { get; set; } = string.Empty;
        public int? ExamenRecuperadoId { get; set; }
        public int? TurnoExamenFinalId { get; set; }
        public decimal NotaMinimaRegularizacion { get; set; }
        public decimal? NotaMinimaPromocion { get; set; }
    }

    public class ExamenActualizarDto
    {
        public int IdMateria { get; set; }
        public int CicloLectivo { get; set; }
        public int IdDocente { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoExamen { get; set; } = "Parcial";
        public int? ExamenRecuperadoId { get; set; }
        public int? TurnoExamenFinalId { get; set; }
    }

    public class ExamenFechaActualizarDto
    {
        public DateTime Fecha { get; set; }
    }

    public class CriteriosEvaluacionDto
    {
        public decimal NotaMinimaRegularizacion { get; set; }
        public decimal? NotaMinimaPromocion { get; set; }
    }
}
