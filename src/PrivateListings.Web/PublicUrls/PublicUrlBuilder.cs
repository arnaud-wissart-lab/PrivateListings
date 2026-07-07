using Microsoft.Extensions.Options;

namespace PrivateListings.Web.PublicUrls;

public sealed class PublicUrlBuilder(IOptions<PrivateListingsPublicUrlOptions> options)
    : IPublicUrlBuilder
{
    public string BuildAbsoluteUrl(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!TryGetPublicBaseUrl(out var publicBaseUrl))
        {
            throw new InvalidOperationException(
                "PrivateListings:PublicBaseUrl doit être configuré pour générer les liens publics.");
        }

        var normalizedPath = path[0] == '/'
            ? path
            : "/" + path;

        return publicBaseUrl + normalizedPath;
    }

    private bool TryGetPublicBaseUrl(out string publicBaseUrl)
    {
        publicBaseUrl = string.Empty;
        var configuredUrl = options.Value.PublicBaseUrl;

        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrWhiteSpace(uri.Query)
            || !string.IsNullOrWhiteSpace(uri.Fragment))
        {
            return false;
        }

        publicBaseUrl = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return true;
    }
}
