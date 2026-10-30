using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HosvetKardex.Web.Services;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "mail.numericasoftware.com";
    public int Puerto { get; set; } = 8889;
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string NombreRemitente { get; set; } = "Sistema Veterinario";
    public int TimeoutMs { get; set; } = 15000;
    public string Seguridad { get; set; } = "Auto";
}

public interface IEmailService
{
    Task EnviarAsync(
        string destinatario,
        string asunto,
        string contenidoHtml,
        CancellationToken cancellationToken = default);
}

public sealed class EmailService(
    IOptions<SmtpOptions> options,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly SmtpOptions _options = options.Value;

    public async Task EnviarAsync(
        string destinatario,
        string asunto,
        string contenidoHtml,
        CancellationToken cancellationToken = default)
    {
        ValidarConfiguracion();

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_options.NombreRemitente, _options.Usuario));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;
        mensaje.Body = new BodyBuilder { HtmlBody = contenidoHtml }.ToMessageBody();

        using var cliente = new SmtpClient { Timeout = _options.TimeoutMs };
        try
        {
            var seguridad = ParseSeguridad(_options.Seguridad);
            await cliente.ConnectAsync(_options.Host, _options.Puerto, seguridad, cancellationToken);
            await cliente.AuthenticateAsync(_options.Usuario, _options.Password, cancellationToken);
            await cliente.SendAsync(mensaje, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el correo SMTP a {Destinatario}.", destinatario);
            throw;
        }
        finally
        {
            if (cliente.IsConnected)
                await cliente.DisconnectAsync(true, cancellationToken);
        }
    }

    private void ValidarConfiguracion()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("No se configuró el host SMTP.");
        if (string.IsNullOrWhiteSpace(_options.Usuario) || string.IsNullOrWhiteSpace(_options.Password))
            throw new InvalidOperationException("No se configuraron las credenciales SMTP.");
        if (_options.Puerto is < 1 or > 65535)
            throw new InvalidOperationException("El puerto SMTP configurado no es válido.");
    }

    private static SecureSocketOptions ParseSeguridad(string? valor) =>
        Enum.TryParse<SecureSocketOptions>(valor, true, out var seguridad)
            ? seguridad
            : SecureSocketOptions.Auto;
}
