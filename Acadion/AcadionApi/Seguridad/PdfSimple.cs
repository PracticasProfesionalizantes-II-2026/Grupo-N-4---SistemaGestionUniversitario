using System.Globalization;
using System.Text;

namespace AcadionApi.Seguridad;

public static class PdfSimple
{
    public static byte[] Crear(string titulo, IEnumerable<string> lineas)
    {
        var paginas = lineas.Prepend(titulo).Chunk(42).ToList();
        var objetos = new List<byte[]>();
        objetos.Add([]); // catálogo
        objetos.Add([]); // árbol de páginas
        objetos.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        var idsPaginas = new List<int>();

        foreach (var pagina in paginas)
        {
            var contenido = CrearContenido(pagina, titulo);
            var contenidoId = objetos.Count + 1;
            objetos.Add(Encoding.ASCII.GetBytes($"<< /Length {contenido.Length} >>\nstream\n{contenido}\nendstream"));
            var paginaId = objetos.Count + 1;
            idsPaginas.Add(paginaId);
            objetos.Add(Encoding.ASCII.GetBytes(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contenidoId} 0 R >>"));
        }

        objetos[0] = Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>");
        objetos[1] = Encoding.ASCII.GetBytes(
            $"<< /Type /Pages /Kids [{string.Join(' ', idsPaginas.Select(id => $"{id} 0 R"))}] /Count {idsPaginas.Count} >>");

        using var output = new MemoryStream();
        Escribir(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objetos.Count; i++)
        {
            offsets.Add(output.Position);
            Escribir(output, $"{i + 1} 0 obj\n");
            output.Write(objetos[i]);
            Escribir(output, "\nendobj\n");
        }
        var xref = output.Position;
        Escribir(output, $"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            Escribir(output, $"{offset:D10} 00000 n \n");
        Escribir(output, $"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

    private static string CrearContenido(IEnumerable<string> lineas, string titulo)
    {
        var builder = new StringBuilder("BT\n/F1 12 Tf\n50 790 Td\n");
        var primero = true;
        foreach (var linea in lineas)
        {
            var limpio = Limpiar(linea);
            if (!primero) builder.Append("0 -18 Td\n");
            if (primero && limpio == Limpiar(titulo)) builder.Append("/F1 18 Tf\n");
            else if (!primero) builder.Append("/F1 11 Tf\n");
            builder.Append('(').Append(Escapar(limpio)).Append(") Tj\n");
            primero = false;
        }
        builder.Append("ET");
        return builder.ToString();
    }

    private static string Limpiar(string texto)
    {
        var normalizado = (texto ?? string.Empty).Normalize(NormalizationForm.FormD);
        return new string(normalizado.Where(c =>
            CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && c <= 255).ToArray())
            .Normalize(NormalizationForm.FormC);
    }

    private static string Escapar(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static void Escribir(Stream stream, string value) => stream.Write(Encoding.ASCII.GetBytes(value));
}
