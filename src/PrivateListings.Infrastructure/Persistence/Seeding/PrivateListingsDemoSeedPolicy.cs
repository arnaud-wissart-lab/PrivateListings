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
            if (!options.AllowInProduction)
            {
                throw new InvalidOperationException(
                    "Le seed de démonstration est activé en production sans autorisation explicite.");
            }

            return true;
        }

        return environment.IsDevelopment() || environment.IsEnvironment("Demo");
    }
}
