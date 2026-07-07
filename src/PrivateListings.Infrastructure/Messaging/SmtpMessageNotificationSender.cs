using System.Data.Common;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrivateListings.Application.Messaging;
using PrivateListings.Infrastructure.Identity;

namespace PrivateListings.Infrastructure.Messaging;

public sealed class SmtpMessageNotificationSender(
    IOptions<PrivateListingsEmailOptions> options,
    IConfiguration configuration,
    ILogger<SmtpMessageNotificationSender> logger)
    : IMessageNotificationSender
{
    private static readonly Action<ILogger, string, Exception?> MessageNotificationNotSent =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(MessageNotificationNotSent)),
            "Notification de messagerie non envoyée car aucun serveur SMTP n'est configuré. Conversation: {ConversationId}");

    private static readonly Action<ILogger, string, Exception?> MessageNotificationSent =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2, nameof(MessageNotificationSent)),
            "Notification de messagerie envoyée. Conversation: {ConversationId}");

    private static readonly Action<ILogger, string, Exception?> MessageNotificationFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(3, nameof(MessageNotificationFailed)),
            "Notification de messagerie non envoyée après erreur SMTP. Conversation: {ConversationId}");

    public async Task SendNewMessageNotificationAsync(
        MessageNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var conversationId = notification.ConversationId.Value.ToString("D");
        if (!TryResolveSmtpEndpoint(out var host, out var port))
        {
            MessageNotificationNotSent(logger, conversationId, null);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(options.Value.FromAddress, options.Value.FromName),
            Subject = "Nouveau message sur PrivateListings",
            Body = BuildBody(notification.ConversationUrl),
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(notification.RecipientEmailAddress));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = options.Value.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(options.Value.UserName))
        {
            client.Credentials = new NetworkCredential(options.Value.UserName, options.Value.Password);
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
            MessageNotificationSent(logger, conversationId, null);
        }
        catch (SmtpException exception)
        {
            MessageNotificationFailed(logger, conversationId, exception);
        }
        catch (InvalidOperationException exception)
        {
            MessageNotificationFailed(logger, conversationId, exception);
        }
    }

    private static string BuildBody(string conversationUrl)
    {
        var encodedUrl = HtmlEncoder.Default.Encode(conversationUrl);

        return
            "<p>Bonjour,</p>"
            + "<p>Vous avez reçu un nouveau message privé sur PrivateListings.</p>"
            + $"<p><a href=\"{encodedUrl}\">Ouvrir PrivateListings</a></p>"
            + "<p>Par confidentialité, cette notification ne contient aucun extrait de la conversation.</p>";
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
