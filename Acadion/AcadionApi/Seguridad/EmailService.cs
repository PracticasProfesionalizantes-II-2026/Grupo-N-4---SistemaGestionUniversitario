using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace AcadionApi.Seguridad;

public interface IEmailService
{
    bool EstaConfigurado { get; }
    Task EnviarAsync(string destinatario, string asunto, string cuerpoTexto);
}

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;

    public SmtpEmailService(IOptions<SmtpOptions> options) => _options = options.Value;

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(_options.Host) &&
        !string.IsNullOrWhiteSpace(_options.Usuario) &&
        !string.IsNullOrWhiteSpace(_options.Password) &&
        !string.IsNullOrWhiteSpace(_options.Remitente);

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoTexto)
    {
        if (!EstaConfigurado)
            throw new InvalidOperationException("El servicio de correo no está configurado.");

        using var mensaje = new MailMessage
        {
            From = new MailAddress(_options.Remitente, _options.NombreRemitente),
            Subject = asunto,
            Body = cuerpoTexto,
            IsBodyHtml = false
        };
        mensaje.To.Add(destinatario);

        using var cliente = new SmtpClient(_options.Host, _options.Puerto)
        {
            EnableSsl = _options.UsarSsl,
            Credentials = new NetworkCredential(_options.Usuario, _options.Password)
        };
        await cliente.SendMailAsync(mensaje);
    }
}
