using System.Collections.Generic;

namespace AcadionApi.DTOs
{
    public class MateriaCrearDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Modalidad { get; set; } = string.Empty; 
        public string Estado { get; set; } = string.Empty;
        public string TipoCursada { get; set; } = "Anual";
        public int? NumeroPeriodo { get; set; }
        public int IdAnio { get; set; }
        public List<int> CorrelativasIds { get; set; } = new List<int>();
    }

    public class MateriaDto
    {
        public int IdMateria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Modalidad { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoCursada { get; set; } = "Anual";
        public int? NumeroPeriodo { get; set; }
        public int IdAnio { get; set; }
        public int IdCarrera { get; set; }
        public string Carrera { get; set; } = string.Empty;
        public int NumeroAnio { get; set; }
        public List<MateriaResumenDto> Correlativas { get; set; } = new List<MateriaResumenDto>();
    }

    public class MateriaListaDto
    {
        public int IdMateria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Modalidad { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoCursada { get; set; } = "Anual";
        public int? NumeroPeriodo { get; set; }
        public int IdAnio { get; set; }
        public int IdCarrera { get; set; }
        public string Carrera { get; set; } = string.Empty;
        public int NumeroAnio { get; set; }
        public List<MateriaResumenDto> Correlativas { get; set; } = new();
    }

    public class MateriaActualizarDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Modalidad { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoCursada { get; set; } = "Anual";
        public int? NumeroPeriodo { get; set; }
        public int IdAnio { get; set; }
        public List<int> CorrelativasIds { get; set; } = new List<int>();
    }

    public class MateriaResumenDto
    {
        public int IdMateria { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
