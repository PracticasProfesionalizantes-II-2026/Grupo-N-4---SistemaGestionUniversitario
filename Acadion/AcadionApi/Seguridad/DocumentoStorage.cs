namespace AcadionApi.Seguridad;

public sealed class DocumentoStorage
{
    private static readonly Dictionary<string, string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = ".pdf",
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png"
    };

    private const long MaximoBytes = 8 * 1024 * 1024;
    public string RutaRaiz { get; }

    public DocumentoStorage(string rutaRaiz)
    {
        RutaRaiz = Path.GetFullPath(rutaRaiz);
        Directory.CreateDirectory(RutaRaiz);
    }

    public async Task<string> GuardarAsync(IFormFile archivo, string carpeta, CancellationToken cancellationToken)
    {
        if (archivo.Length <= 0) throw new ArgumentException("El archivo está vacío.");
        if (archivo.Length > MaximoBytes) throw new ArgumentException("El archivo no puede superar 8 MB.");
        if (!ExtensionesPermitidas.TryGetValue(archivo.ContentType, out var extension))
            throw new ArgumentException("Solo se admiten archivos PDF, JPG o PNG.");

        await using var origen = archivo.OpenReadStream();
        var cabecera = new byte[8];
        var leidos = await origen.ReadAsync(cabecera, cancellationToken);
        if (!FirmaValida(archivo.ContentType, cabecera.AsSpan(0, leidos)))
            throw new ArgumentException("El contenido del archivo no coincide con un PDF, JPG o PNG válido.");
        origen.Position = 0;

        var rutaCarpeta = Path.Combine(RutaRaiz, carpeta);
        Directory.CreateDirectory(rutaCarpeta);
        var nombre = $"{Guid.NewGuid():N}{extension}";
        var ruta = Path.Combine(rutaCarpeta, nombre);
        await using var destino = File.Create(ruta);
        await origen.CopyToAsync(destino, cancellationToken);
        return Path.Combine(carpeta, nombre).Replace('\\', '/');
    }

    private static bool FirmaValida(string contentType, ReadOnlySpan<byte> bytes) => contentType switch
    {
        "application/pdf" => bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8),
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255,
        _ => false
    };

    public (string Ruta, string ContentType)? Obtener(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa) || Uri.IsWellFormedUriString(rutaRelativa, UriKind.Absolute))
            return null;
        var ruta = Path.GetFullPath(Path.Combine(RutaRaiz, rutaRelativa.Replace('/', Path.DirectorySeparatorChar)));
        if (!ruta.StartsWith(RutaRaiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(ruta))
            return null;
        var contentType = Path.GetExtension(ruta).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            _ => "image/jpeg"
        };
        return (ruta, contentType);
    }
}
