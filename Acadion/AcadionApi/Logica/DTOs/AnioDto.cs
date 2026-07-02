namespace AcadionApi.DTOs
{
    public class AnioCrearDto
    {
        public int NumeroAnio { get; set; }
        public string NombreAnio { get; set; } = string.Empty;
        public int IdCarrera { get; set; }
    }

    public class AnioDto
    {
        public int IdAnio { get; set; }
        public int NumeroAnio { get; set; }
        public string NombreAnio { get; set; } = string.Empty;
        public int IdCarrera { get; set; }
    }

    public class AnioActualizarDto
    {
        public int NumeroAnio { get; set; }
        public string NombreAnio { get; set; } = string.Empty;
        public int IdCarrera { get; set; }
    }
}