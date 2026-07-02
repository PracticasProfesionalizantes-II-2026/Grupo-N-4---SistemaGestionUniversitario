namespace AcadionApi.DTOs
{
    public class CarreraCrearDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string PlanEstudios { get; set; } = string.Empty;
    }

    public class CarreraDto
    {
        public int IdCarrera { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string PlanEstudios { get; set; } = string.Empty;
    }

    public class CarreraActualizarDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string PlanEstudios { get; set; } = string.Empty;
    }
}