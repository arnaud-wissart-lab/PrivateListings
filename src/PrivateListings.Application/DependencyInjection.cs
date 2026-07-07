using Microsoft.Extensions.DependencyInjection;

namespace PrivateListings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPrivateListingsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
