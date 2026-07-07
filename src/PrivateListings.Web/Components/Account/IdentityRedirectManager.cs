using Microsoft.AspNetCore.Components;

namespace PrivateListings.Web.Components.Account;

internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "PrivateListings.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        SecurePolicy = CookieSecurePolicy.Always,
        MaxAge = TimeSpan.FromSeconds(10)
    };

    public void RedirectTo(string? uri)
    {
        navigationManager.NavigateTo(NormalizeLocalUri(uri));
    }

    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    private string NormalizeLocalUri(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return "/";
        }

        if (Uri.IsWellFormedUriString(uri, UriKind.Relative)
            && !uri.StartsWith("//", StringComparison.Ordinal)
            && uri[0] != '\\')
        {
            return uri;
        }

        try
        {
            var relativeUri = navigationManager.ToBaseRelativePath(uri);
            if (string.IsNullOrWhiteSpace(relativeUri))
            {
                return "/";
            }

            return relativeUri[0] == '/' ? relativeUri : $"/{relativeUri}";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return "/";
        }
    }
}
