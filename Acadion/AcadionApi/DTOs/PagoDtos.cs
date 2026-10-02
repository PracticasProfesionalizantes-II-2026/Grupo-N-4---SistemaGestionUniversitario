namespace AcadionApi.DTOs;

public class EstadoPagoActualizarDto
{
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime? FechaPago { get; set; }
    public string ComprobanteUrl { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}
