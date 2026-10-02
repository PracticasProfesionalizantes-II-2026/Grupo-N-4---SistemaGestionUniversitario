using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AcadionApi.Seguridad;

/// <summary>
/// Aplica reglas de formato y tamaño a todos los cuerpos JSON antes de que
/// lleguen a los endpoints. Las reglas de negocio específicas continúan en
/// cada servicio; esta capa evita que un cliente externo saltee las
/// validaciones del navegador.
/// </summary>
public sealed partial class ValidacionEntradaMiddleware
{
    private const long MaximoCuerpoJson = 1_048_576;
    private const int MaximoTextoGeneral = 5_000;
    private readonly RequestDelegate _siguiente;

    public ValidacionEntradaMiddleware(RequestDelegate siguiente)
    {
        _siguiente = siguiente;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!TieneCuerpoJson(context.Request))
        {
            await _siguiente(context);
            return;
        }

        if (context.Request.ContentLength > MaximoCuerpoJson)
        {
            await RechazarAsync(context, "La solicitud supera el tamaño permitido.",
                StatusCodes.Status413PayloadTooLarge);
            return;
        }

        context.Request.EnableBuffering();
        JsonDocument documento;
        try
        {
            documento = await JsonDocument.ParseAsync(context.Request.Body,
                new JsonDocumentOptions { MaxDepth = 20 });
        }
        catch (JsonException)
        {
            context.Request.Body.Position = 0;
            await RechazarAsync(context, "El formato JSON de la solicitud no es válido.");
            return;
        }

        context.Request.Body.Position = 0;
        using (documento)
        {
            var error = ValidarElemento(documento.RootElement, context.Request.Path, string.Empty);
            if (error is not null)
            {
                await RechazarAsync(context, error);
                return;
            }
        }

        await _siguiente(context);
    }

    private static bool TieneCuerpoJson(HttpRequest request) =>
        HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) ||
        HttpMethods.IsPatch(request.Method)
            ? request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true
            : false;

    private static string? ValidarElemento(JsonElement elemento, PathString ruta, string propiedadPadre)
    {
        switch (elemento.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var propiedad in elemento.EnumerateObject())
                {
                    var error = ValidarPropiedad(propiedad.Name, propiedad.Value, ruta);
                    if (error is not null) return error;
                    error = ValidarElemento(propiedad.Value, ruta, propiedad.Name);
                    if (error is not null) return error;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in elemento.EnumerateArray())
                {
                    var error = ValidarElemento(item, ruta, propiedadPadre);
                    if (error is not null) return error;
                }
                break;
        }

        return null;
    }

    private static string? ValidarPropiedad(string nombre, JsonElement valor, PathString ruta)
    {
        if (valor.ValueKind == JsonValueKind.Null) return null;

        if (nombre.Equals("dni", StringComparison.OrdinalIgnoreCase))
        {
            if (valor.ValueKind != JsonValueKind.Number || !valor.TryGetInt64(out var dni) ||
                dni is < 1_000_000 or > 99_999_999)
                return "El DNI debe contener entre 7 y 8 números.";
            return null;
        }

        if (nombre.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
            valor.ValueKind == JsonValueKind.Number &&
            (!valor.TryGetInt64(out var id) || id <= 0))
            return $"El campo {NombreLegible(nombre)} debe ser un identificador válido.";

        if (nombre.Equals("codigoPostal", StringComparison.OrdinalIgnoreCase) &&
            valor.ValueKind == JsonValueKind.Number &&
            (!valor.TryGetInt32(out var codigoPostal) || codigoPostal is < 0 or > 99_999_999))
            return "El código postal no es válido.";

        if (nombre.Equals("cicloLectivo", StringComparison.OrdinalIgnoreCase) &&
            valor.ValueKind == JsonValueKind.Number &&
            (!valor.TryGetInt32(out var ciclo) || ciclo is < 2020 or > 2100))
            return "El ciclo lectivo debe estar comprendido entre 2020 y 2100.";

        if (valor.ValueKind != JsonValueKind.String) return null;
        var texto = valor.GetString() ?? string.Empty;
        if (texto.Length > MaximoTextoGeneral)
            return $"El campo {NombreLegible(nombre)} es demasiado extenso.";
        if (texto.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t'))
            return $"El campo {NombreLegible(nombre)} contiene caracteres de control no permitidos.";

        if (EsPassword(nombre))
            return texto.Length > 256 ? "La contraseña supera el tamaño permitido." : null;

        if (EsNombreDePersona(nombre, ruta))
        {
            if (texto.Length is < 2 or > 80 || !NombrePersonaRegex().IsMatch(texto.Trim()))
                return $"El campo {NombreLegible(nombre)} solo puede contener letras, espacios y guiones.";
            return null;
        }

        if (nombre.Equals("nombreUsuario", StringComparison.OrdinalIgnoreCase))
        {
            if (texto.Length is < 3 or > 80 || !NombreUsuarioRegex().IsMatch(texto.Trim()))
                return "El nombre de usuario solo puede contener letras, números y puntos.";
            return null;
        }

        if (EsCorreo(nombre) && texto.Length > 0)
        {
            if (texto.Length > 254 || !MailAddress.TryCreate(texto.Trim(), out var correo) ||
                !correo.Address.Equals(texto.Trim(), StringComparison.OrdinalIgnoreCase))
                return $"El campo {NombreLegible(nombre)} no contiene un correo electrónico válido.";
            return null;
        }

        if (EsTelefono(nombre) && texto.Length > 0)
        {
            if (!TelefonoRegex().IsMatch(texto.Trim()))
                return $"El campo {NombreLegible(nombre)} solo puede contener números, espacios, paréntesis, + y guiones.";
            return null;
        }

        if (EsUrl(nombre) && texto.Length > 0)
        {
            if (texto.Length > 2_048 || !Uri.TryCreate(texto, UriKind.Absolute, out var url) ||
                url.Scheme is not ("http" or "https"))
                return $"El campo {NombreLegible(nombre)} debe contener una dirección web válida.";
            return null;
        }

        if (EsTextoAcademico(nombre, ruta) && texto.Length > 0)
        {
            if (texto.Length > 150 || !TextoAcademicoRegex().IsMatch(texto.Trim()))
                return $"El campo {NombreLegible(nombre)} contiene caracteres no permitidos.";
            return null;
        }

        var maximo = MaximoPorCampo(nombre);
        if (texto.Length > maximo)
            return $"El campo {NombreLegible(nombre)} no puede superar los {maximo} caracteres.";

        return null;
    }

    private static bool EsNombreDePersona(string nombre, PathString ruta)
    {
        if (nombre.Equals("apellido", StringComparison.OrdinalIgnoreCase) ||
            nombre.Equals("nombreApellido", StringComparison.OrdinalIgnoreCase))
            return true;
        if (!nombre.Equals("nombre", StringComparison.OrdinalIgnoreCase)) return false;
        var path = ruta.Value ?? string.Empty;
        return path.StartsWith("/api/gestion/usuarios", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/api/auth/bootstrap-secretario", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/personas", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EsTextoAcademico(string nombre, PathString ruta) =>
        nombre.Equals("localidad", StringComparison.OrdinalIgnoreCase) ||
        nombre.Equals("especialidad", StringComparison.OrdinalIgnoreCase) ||
        nombre.Equals("tituloAcademico", StringComparison.OrdinalIgnoreCase) ||
        nombre.Equals("planEstudios", StringComparison.OrdinalIgnoreCase) ||
        nombre.Equals("nombreAnio", StringComparison.OrdinalIgnoreCase) ||
        (nombre.Equals("nombre", StringComparison.OrdinalIgnoreCase) && !EsNombreDePersona(nombre, ruta));

    private static bool EsCorreo(string nombre) =>
        nombre.Contains("email", StringComparison.OrdinalIgnoreCase) ||
        nombre.Equals("correo", StringComparison.OrdinalIgnoreCase) ||
        nombre.Equals("gmail", StringComparison.OrdinalIgnoreCase);

    private static bool EsTelefono(string nombre) =>
        nombre.Contains("telefono", StringComparison.OrdinalIgnoreCase) ||
        nombre.Equals("celular", StringComparison.OrdinalIgnoreCase);

    private static bool EsPassword(string nombre) =>
        nombre.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        nombre.Equals("contraseña", StringComparison.OrdinalIgnoreCase);

    private static bool EsUrl(string nombre) =>
        nombre.EndsWith("Url", StringComparison.OrdinalIgnoreCase);

    private static int MaximoPorCampo(string nombre) => nombre.ToLowerInvariant() switch
    {
        "titulo" => 120,
        "mensaje" => 2_000,
        "observaciones" => 500,
        "descripcion" => 1_000,
        "temadictado" => 300,
        "direccion" => 180,
        "relacion" => 60,
        "cuatrimestre" => 20,
        "estado" or "tipo" or "tipoexamen" or "tipoclase" or "tipocursada" or
            "modalidad" or "prioridad" or "diasemana" => 40,
        _ => 300
    };

    private static string NombreLegible(string nombre) =>
        Regex.Replace(nombre, "([a-z])([A-Z])", "$1 $2").ToLowerInvariant();

    private static async Task RechazarAsync(HttpContext context, string mensaje,
        int estado = StatusCodes.Status400BadRequest)
    {
        context.Response.StatusCode = estado;
        await context.Response.WriteAsJsonAsync(new { mensaje });
    }

    [GeneratedRegex(@"^[\p{L}\p{M}]+(?:[ -][\p{L}\p{M}]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex NombrePersonaRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex NombreUsuarioRegex();

    [GeneratedRegex(@"^\+?[0-9 ()-]{6,30}$", RegexOptions.CultureInvariant)]
    private static partial Regex TelefonoRegex();

    [GeneratedRegex(@"^[\p{L}\p{M}\p{N} .()/\-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex TextoAcademicoRegex();
}
