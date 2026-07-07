using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PrivateListings.Infrastructure.Persistence.Seeding;

namespace PrivateListings.Infrastructure.Persistence;

public sealed class PrivateListingsDatabaseInitializer(
    PrivateListingsDbContext dbContext,
    PrivateListingsReferenceDataSeeder referenceDataSeeder,
    PrivateListingsDemoDataSeeder demoDataSeeder,
    IHostEnvironment environment,
    IOptions<PrivateListingsPersistenceOptions> persistenceOptions,
    IOptions<PrivateListingsReferenceDataOptions> referenceDataOptions,
    IOptions<PrivateListingsDemoSeedOptions> demoSeedOptions)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(referenceDataSeeder);
        ArgumentNullException.ThrowIfNull(demoDataSeeder);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(persistenceOptions);
        ArgumentNullException.ThrowIfNull(referenceDataOptions);
        ArgumentNullException.ThrowIfNull(demoSeedOptions);

        if (persistenceOptions.Value.ApplyMigrationsOnStartup)
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        if (referenceDataOptions.Value.SeedInitialCategories)
        {
            await referenceDataSeeder.SeedAsync(cancellationToken).ConfigureAwait(false);
        }

        if (PrivateListingsDemoSeedPolicy.ShouldRun(environment, demoSeedOptions.Value))
        {
            await demoDataSeeder.SeedAsync(environment, demoSeedOptions.Value, cancellationToken).ConfigureAwait(false);
        }
    }
}
