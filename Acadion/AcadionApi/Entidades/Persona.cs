using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Persona
{
    [Key]
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Apellido { get; set; } = string.Empty;

    public long Dni { get; set; }

    public DateTime FechaNacimiento { get; set; }

    public string Direccion { get; set; } = string.Empty;

    public string Localidad { get; set; } = string.Empty;

    public int CodigoPostal { get; set; }

    public string Email { get; set; } = string.Empty;

    // Edad calculada automáticamente
    [NotMapped]
    public int Edad
    {
        get
        {
            var edad = DateTime.Today.Year - FechaNacimiento.Year;

            if (FechaNacimiento.Date > DateTime.Today.AddYears(-edad))
                edad--;

            return edad;
        }
    }
}