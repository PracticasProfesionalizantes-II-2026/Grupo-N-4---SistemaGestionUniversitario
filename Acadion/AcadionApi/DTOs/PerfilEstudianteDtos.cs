using System.ComponentModel.DataAnnotations;

namespace AcadionApi.DTOs;

public class PerfilPropioActualizarDto
{
    [EmailAddress, MaxLength(160)]
    public string EmailPersonal { get; set; } = string.Empty;

    [MaxLength(40)]
    public string TelefonoContacto { get; set; } = string.Empty;

    [MaxLength(180)]
    public string Direccion { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Localidad { get; set; } = string.Empty;

    [Range(0, 99999999)]
    public int CodigoPostal { get; set; }
}

public class FinanciamientoPropioDto
{
    public bool AporteFamiliares { get; set; }
    public bool PlanesSociales { get; set; }
    public bool Trabajo { get; set; }
    public bool Beca { get; set; }
    public bool OtraFuente { get; set; }
}

public class AllegadoPropioDto
{
    [Required, MaxLength(120)]
    public string NombreApellido { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string Relacion { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;
}

public class AllegadosPropiosActualizarDto
{
    public List<AllegadoPropioDto> Allegados { get; set; } = [];
}
