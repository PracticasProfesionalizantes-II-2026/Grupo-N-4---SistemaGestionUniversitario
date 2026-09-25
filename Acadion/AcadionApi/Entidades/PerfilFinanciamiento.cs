using System.ComponentModel.DataAnnotations;

public class PerfilFinanciamiento
{
    [Key]
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public bool AporteFamiliares { get; set; }
    public bool PlanesSociales { get; set; }
    public bool Trabajo { get; set; }
    public bool Beca { get; set; }
    public bool OtraFuente { get; set; }
}
