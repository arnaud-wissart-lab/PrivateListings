using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrivateListings.Infrastructure.Persistence;
using PrivateListings.Infrastructure.Persistence.Seeding;

namespace PrivateListings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPrivateListingsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .Configure<PrivateListingsPersistenceOptions>(
                configuration.GetSection(PrivateListingsPersistenceOptions.SectionName))
            .Configure<PrivateListingsReferenceDataOptions>(
                configuration.GetSection(PrivateListingsReferenceDataOptions.SectionName))
            .Configure<PrivateListingsDemoSeedOptions>(
                configuration.GetSection(PrivateListingsDemoSeedOptions.SectionName));

        if (environment is not null)
        {
            services.AddSingleton(environment);
        }

        var persistenceOptions = configuration
            .GetSection(PrivateListingsPersistenceOptions.SectionName)
            .Get<PrivateListingsPersistenceOptions>()
            ?? new PrivateListingsPersistenceOptions();

        var connectionName = string.IsNullOrWhiteSpace(persistenceOptions.ConnectionName)
            ? "privatelistingsdb"
            : persistenceOptions.ConnectionName;

        var connectionString = configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException(
                $"La chaîne de connexion '{connectionName}' est introuvable.");

        services.AddDbContext<PrivateListingsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(PrivateListingsDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                }));

        services.AddScoped<PrivateListingsReferenceDataSeeder>();
        services.AddScoped<PrivateListingsDemoDataSeeder>();
        services.AddScoped<PrivateListingsDatabaseInitializer>();

        return services;
    }
}
