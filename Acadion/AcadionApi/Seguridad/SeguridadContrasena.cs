namespace AcadionApi.Seguridad;

public static class SeguridadContrasena
{
    public static void Validar(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
            throw new ArgumentException("La contraseña debe tener al menos 10 caracteres.");
        if (password.Length > 128)
            throw new ArgumentException("La contraseña no puede superar los 128 caracteres.");
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            throw new ArgumentException("La contraseña debe incluir mayúsculas, minúsculas y números.");
    }
}
