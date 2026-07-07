using Microsoft.Extensions.DependencyInjection;
using PrivateListings.Application.Authentication;
using PrivateListings.Application.Listings;

namespace PrivateListings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPrivateListingsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AccountPasswordPolicyOptions>();
        services.AddOptions<AccountRateLimitOptions>();
        services.AddOptions<ListingImageOptions>();
        services.AddOptions<ListingRateLimitOptions>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAccountPasswordPolicy, AccountPasswordPolicy>();
        services.AddSingleton<IAccountRateLimiter, InMemoryAccountRateLimiter>();
        services.AddSingleton<IListingRateLimiter, InMemoryListingRateLimiter>();
        services.AddScoped<IListingService, ListingService>();

        return services;
    }
}
