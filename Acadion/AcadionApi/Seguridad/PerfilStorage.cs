namespace AcadionApi.Seguridad;

public sealed class PerfilStorage
{
    public PerfilStorage(string directorio) => Directorio = directorio;
    public string Directorio { get; }
}
