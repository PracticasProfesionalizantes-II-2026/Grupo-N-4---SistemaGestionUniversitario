using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AcadionApi.Logica;

public static class NormalizadorDatos
{
    private static readonly TextInfo TextoEspanol = CultureInfo.GetCultureInfo("es-AR").TextInfo;
    private static readonly Regex NombrePersonaValido = new(
        @"^[\p{L}\p{M}]+(?:[ -][\p{L}\p{M}]+)*$", RegexOptions.Compiled);
    private static readonly Regex TextoAcademicoValido = new(
        @"^[\p{L}\p{M}\p{N} .()/\-]+$", RegexOptions.Compiled);

    public static string NombrePropio(string? valor)
    {
        var limpio = Espacios(valor);
        return TextoEspanol.ToTitleCase(limpio.ToLower(CultureInfo.GetCultureInfo("es-AR")));
    }

    public static string Correo(string? valor) =>
        Espacios(valor).ToLowerInvariant();

    public static string NombreUsuario(string? valor)
    {
        var texto = Espacios(valor).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sinAcentos = new string(texto
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        return Regex.Replace(sinAcentos, "[^a-z0-9.]+", string.Empty).Trim('.');
    }

    public static string Espacios(string? valor) =>
        Regex.Replace(valor?.Trim() ?? string.Empty, @"\s+", " ");

    public static void ValidarNombrePersona(string? valor, string campo)
    {
        var limpio = Espacios(valor);
        if (!NombrePersonaValido.IsMatch(limpio))
            throw new ArgumentException(
                $"{campo} solo puede contener letras, espacios y guiones.");
    }

    public static void ValidarTextoAcademico(string? valor, string campo)
    {
        var limpio = Espacios(valor);
        if (!TextoAcademicoValido.IsMatch(limpio))
            throw new ArgumentException(
                $"{campo} contiene caracteres no permitidos. Usá letras, números, espacios y separadores académicos comunes.");
    }
}
