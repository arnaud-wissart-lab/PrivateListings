using Microsoft.EntityFrameworkCore;
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
