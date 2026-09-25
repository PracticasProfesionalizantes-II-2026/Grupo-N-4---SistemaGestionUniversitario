public class Permiso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}
