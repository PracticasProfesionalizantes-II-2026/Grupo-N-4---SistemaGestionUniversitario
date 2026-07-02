namespace AcadionApi.DTOs
{
    public class NotaExamenCrearDto
    {
        public int IdExamen { get; set; }
        public int IdEstudiante { get; set; }
        public decimal Nota { get; set; }
        public string? Observaciones { get; set; }
    }

    public class NotaExamenDto
    {
        public int IdNota { get; set; }
        public int IdExamen { get; set; }
        public int IdEstudiante { get; set; }
        public decimal Nota { get; set; }
        public string? Observaciones { get; set; }
    }

    public class NotaExamenActualizarDto
    {
        public int IdExamen { get; set; }
        public int IdEstudiante { get; set; }
        public decimal Nota { get; set; }
        public string? Observaciones { get; set; }
    }
}