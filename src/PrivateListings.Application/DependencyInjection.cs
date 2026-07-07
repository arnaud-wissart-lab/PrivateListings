using Microsoft.Extensions.DependencyInjection;
using PrivateListings.Application.Authentication;

namespace PrivateListings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPrivateListingsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AccountPasswordPolicyOptions>();
        services.AddOptions<AccountRateLimitOptions>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAccountPasswordPolicy, AccountPasswordPolicy>();
        services.AddSingleton<IAccountRateLimiter, InMemoryAccountRateLimiter>();

        return services;
    }
}
