using Microsoft.Extensions.Hosting;

namespace PrivateListings.Infrastructure.Persistence.Seeding;

public static class PrivateListingsDemoSeedPolicy
{
    public static bool ShouldRun(IHostEnvironment environment, PrivateListingsDemoSeedOptions options)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return false;
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Le seed de démonstration ne peut pas être exécuté en production.");
        }

        return environment.IsDevelopment() || environment.IsEnvironment("Demo");
    }
}
