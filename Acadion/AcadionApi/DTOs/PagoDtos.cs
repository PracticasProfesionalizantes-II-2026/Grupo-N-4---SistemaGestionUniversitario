namespace AcadionApi.DTOs;

public class EstadoPagoActualizarDto
{
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime? FechaPago { get; set; }
    public string ComprobanteUrl { get; set; } = string.Empty;
}
