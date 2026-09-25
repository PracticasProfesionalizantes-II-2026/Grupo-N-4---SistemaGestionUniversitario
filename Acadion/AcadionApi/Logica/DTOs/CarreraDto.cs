namespace AcadionApi.DTOs
{
    public class CarreraCrearDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string PlanEstudios { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int DuracionAnios { get; set; }
        public int CapacidadMaximaEstudiantes { get; set; }
    }

    public class CarreraDto
    {
        public int IdCarrera { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string PlanEstudios { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int DuracionAnios { get; set; }
        public int CapacidadMaximaEstudiantes { get; set; }
        public int EstudiantesInscriptos { get; set; }
        public List<AnioCarreraDto> Anios { get; set; } = new();
    }

    public class CarreraActualizarDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string PlanEstudios { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int DuracionAnios { get; set; }
        public int CapacidadMaximaEstudiantes { get; set; }
    }

    public class AnioCarreraDto
    {
        public int IdAnio { get; set; }
        public int NumeroAnio { get; set; }
        public string NombreAnio { get; set; } = string.Empty;
    }
}
