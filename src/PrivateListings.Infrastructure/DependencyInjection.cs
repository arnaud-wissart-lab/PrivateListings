using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrivateListings.Infrastructure.Persistence;

namespace PrivateListings.Infrastructure;

public static class DependencyInjection
{
    private const string DefaultConnectionName = "privatelistingsdb";

    public static IServiceCollection AddPrivateListingsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(DefaultConnectionName)
            ?? throw new InvalidOperationException(
                $"La chaîne de connexion '{DefaultConnectionName}' est introuvable.");

        services.AddDbContext<PrivateListingsDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
