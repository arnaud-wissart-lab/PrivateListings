using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrivateListings.Application;
using PrivateListings.Infrastructure;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PrivateListings.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("privatelistings_tests")
        .WithUsername("privatelistings")
        .WithPassword("privatelistings_tests")
        .Build();

    public async Task InitializeAsync()
        => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync()
        => await _container.DisposeAsync().AsTask().ConfigureAwait(false);

    public PrivateListingsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PrivateListingsDbContext>()
            .UseNpgsql(
                _container.GetConnectionString(),
                npgsql => npgsql.MigrationsAssembly(typeof(PrivateListingsDbContext).Assembly.FullName))
            .Options;

        return new PrivateListingsDbContext(options);
    }

    public ServiceProvider CreateServiceProvider(
        IHostEnvironment? environment = null,
        Action<IServiceCollection>? configureServices = null,
        bool validateOnBuild = false)
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services
            .AddPrivateListingsApplication()
            .AddPrivateListingsInfrastructure(configuration, environment ?? new TestHostEnvironment("Tests"));
        services.AddHttpContextAccessor();
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        }).AddIdentityCookies();
        services.AddLogging();

        configureServices?.Invoke(services);

        return BuildServiceProvider(services, validateOnBuild);
    }

    public ServiceProvider CreateWorkerServiceProvider(
        IHostEnvironment? environment = null,
        Action<IServiceCollection>? configureServices = null,
        bool validateOnBuild = false)
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services
            .AddPrivateListingsApplication()
            .AddPrivateListingsWorkerInfrastructure(configuration, environment ?? new TestHostEnvironment("Tests"));
        services.AddLogging();

        configureServices?.Invoke(services);

        return BuildServiceProvider(services, validateOnBuild);
    }

    private IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:privatelistingsdb"] = _container.GetConnectionString(),
                ["PrivateListings:Persistence:ConnectionName"] = "privatelistingsdb",
                ["PrivateListings:Persistence:ApplyMigrationsOnStartup"] = "false",
                ["PrivateListings:ReferenceData:SeedInitialCategories"] = "false",
                ["PrivateListings:DemoSeed:Enabled"] = "false"
            })
            .Build();

    private static ServiceProvider BuildServiceProvider(IServiceCollection services, bool validateOnBuild)
        => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = validateOnBuild,
            ValidateOnBuild = validateOnBuild
        });

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlTestCollectionDefinition : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSql";
}
