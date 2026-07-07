using System.Data.Common;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PrivateListings.Infrastructure.Identity;

public sealed class PrivateListingsIdentityEmailSender(
    IOptions<PrivateListingsEmailOptions> options,
    IConfiguration configuration,
    ILogger<PrivateListingsIdentityEmailSender> logger)
    : IEmailSender<PrivateListingsUser>
{
    private static readonly Action<ILogger, string, Exception?> SecurityEmailNotSent =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(SecurityEmailNotSent)),
            "Email de sécurité non envoyé car aucun serveur SMTP n'est configuré. Sujet: {Subject}");

    private static readonly Action<ILogger, string, Exception?> SecurityEmailSent =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2, nameof(SecurityEmailSent)),
            "Email de sécurité envoyé. Sujet: {Subject}");

    public Task SendConfirmationLinkAsync(
        PrivateListingsUser user,
        string email,
        string confirmationLink)
        => SendSecurityEmailAsync(
            email,
            "Confirmez votre adresse email",
            $"<p>Bonjour,</p><p>Confirmez votre adresse email PrivateListings avec ce lien temporaire :</p><p><a href=\"{HtmlEncoder.Default.Encode(confirmationLink)}\">Confirmer mon email</a></p>");

    public Task SendPasswordResetLinkAsync(
        PrivateListingsUser user,
        string email,
        string resetLink)
        => SendSecurityEmailAsync(
            email,
            "Réinitialisation de votre mot de passe",
            $"<p>Bonjour,</p><p>Vous pouvez définir un nouveau mot de passe PrivateListings avec ce lien temporaire :</p><p><a href=\"{HtmlEncoder.Default.Encode(resetLink)}\">Réinitialiser mon mot de passe</a></p>");

    public Task SendPasswordResetCodeAsync(
        PrivateListingsUser user,
        string email,
        string resetCode)
        => SendSecurityEmailAsync(
            email,
            "Code de réinitialisation de votre mot de passe",
            "<p>Bonjour,</p><p>Un code de réinitialisation a été généré pour votre compte PrivateListings.</p>");

    private async Task SendSecurityEmailAsync(string to, string subject, string htmlBody)
    {
        if (!TryResolveSmtpEndpoint(out var host, out var port))
        {
            SecurityEmailNotSent(logger, subject, null);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(options.Value.FromAddress, options.Value.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(to));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = options.Value.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(options.Value.UserName))
        {
            client.Credentials = new NetworkCredential(options.Value.UserName, options.Value.Password);
        }

        await client.SendMailAsync(message).ConfigureAwait(false);
        SecurityEmailSent(logger, subject, null);
    }

    private bool TryResolveSmtpEndpoint(out string host, out int port)
    {
        host = options.Value.SmtpHost
            ?? Environment.GetEnvironmentVariable("MAILPIT_SMTPHOST")
            ?? Environment.GetEnvironmentVariable("MAILPIT_HOST")
            ?? string.Empty;

        port = options.Value.SmtpPort
            ?? TryReadPort(Environment.GetEnvironmentVariable("MAILPIT_SMTPPORT"))
            ?? TryReadPort(Environment.GetEnvironmentVariable("MAILPIT_PORT"))
            ?? 1025;

        if (!string.IsNullOrWhiteSpace(host))
        {
            return true;
        }

        var connectionString = configuration.GetConnectionString("mailpit");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (builder.TryGetValue("endpoint", out var endpoint)
                && Uri.TryCreate(endpoint?.ToString(), UriKind.Absolute, out var endpointUri)
                && !string.IsNullOrWhiteSpace(endpointUri.Host))
            {
                host = endpointUri.Host;
                port = endpointUri.Port > 0 ? endpointUri.Port : port;
                return true;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        return false;
    }

    private static int? TryReadPort(string? value)
        => int.TryParse(value, out var port) ? port : null;
}
