using Microsoft.Extensions.Options;

namespace PrivateListings.Web;

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UsePrivateListingsSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            var options = context.RequestServices.GetRequiredService<IOptions<SecurityHeadersOptions>>().Value;
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            if (!string.IsNullOrWhiteSpace(options.ContentSecurityPolicy))
            {
                headers.TryAdd("Content-Security-Policy", options.ContentSecurityPolicy);
            }

            headers.TryAdd(
                "Permissions-Policy",
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

            await next(context).ConfigureAwait(false);
        });
    }
}
