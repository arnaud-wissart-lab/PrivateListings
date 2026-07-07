namespace PrivateListings.Web;

public sealed class SecurityHeadersOptions
{
    public const string SectionName = "PrivateListings:SecurityHeaders";

    public string? ContentSecurityPolicy { get; set; } =
        "default-src 'self'; "
        + "base-uri 'self'; "
        + "object-src 'none'; "
        + "frame-ancestors 'none'; "
        + "img-src 'self' data: blob:; "
        + "script-src 'self' 'unsafe-inline'; "
        + "style-src 'self' 'unsafe-inline'; "
        + "connect-src 'self' ws: wss:; "
        + "form-action 'self'";
}
