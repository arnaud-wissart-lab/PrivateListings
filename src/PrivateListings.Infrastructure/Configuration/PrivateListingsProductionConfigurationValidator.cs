using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Infrastructure.Persistence.Seeding;

namespace PrivateListings.Infrastructure.Configuration;

public static class PrivateListingsProductionConfigurationValidator
{
    public static void Validate(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool validateAllowedHosts)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (!environment.IsProduction())
        {
            return;
        }

        var errors = new List<string>();

        ValidatePublicBaseUrl(configuration, errors);
        ValidateDemoSeed(configuration, errors);
        ValidateSmtp(configuration, errors);

        if (validateAllowedHosts)
        {
            ValidateAllowedHosts(configuration, errors);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuration Production invalide : " + string.Join(" ", errors));
        }
    }

    private static void ValidatePublicBaseUrl(IConfiguration configuration, List<string> errors)
    {
        var publicBaseUrl = configuration["PrivateListings:PublicBaseUrl"];
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrWhiteSpace(uri.Query)
            || !string.IsNullOrWhiteSpace(uri.Fragment))
        {
            errors.Add("PrivateListings:PublicBaseUrl doit être une URL publique absolue HTTP ou HTTPS sans query string ni fragment.");
        }
    }

    private static void ValidateAllowedHosts(IConfiguration configuration, List<string> errors)
    {
        var allowedHosts = configuration["AllowedHosts"];
        var hosts = allowedHosts?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];

        if (hosts.Length == 0 || hosts.Any(host => host == "*"))
        {
            errors.Add("AllowedHosts doit cibler le ou les hôtes publics et ne doit pas valoir '*'.");
        }
    }

    private static void ValidateSmtp(IConfiguration configuration, List<string> errors)
    {
        var emailOptions = configuration
            .GetSection(PrivateListingsEmailOptions.SectionName)
            .Get<PrivateListingsEmailOptions>()
            ?? new PrivateListingsEmailOptions();

        if (string.IsNullOrWhiteSpace(emailOptions.SmtpHost))
        {
            errors.Add("PrivateListings:Email:SmtpHost est obligatoire en Production pour les emails transactionnels.");
        }
    }

    private static void ValidateDemoSeed(IConfiguration configuration, List<string> errors)
    {
        var demoSeedOptions = configuration
            .GetSection(PrivateListingsDemoSeedOptions.SectionName)
            .Get<PrivateListingsDemoSeedOptions>()
            ?? new PrivateListingsDemoSeedOptions();

        if (demoSeedOptions.Enabled)
        {
            errors.Add("PrivateListings:DemoSeed:Enabled doit rester désactivé en Production.");
        }
    }
}
