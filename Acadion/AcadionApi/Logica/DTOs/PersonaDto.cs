using System;

namespace AcadionApi.DTOs
{
    public class PersonaCrearDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public long Dni { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string Localidad { get; set; } = string.Empty;
        public int CodigoPostal { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public class PersonaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public long Dni { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public int Edad { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string Localidad { get; set; } = string.Empty;
        public int CodigoPostal { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public class PersonaActualizarDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public long Dni { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string Localidad { get; set; } = string.Empty;
        public int CodigoPostal { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}