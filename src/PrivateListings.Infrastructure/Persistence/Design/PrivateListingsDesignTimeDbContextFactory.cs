using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PrivateListings.Infrastructure.Persistence.Design;

public sealed class PrivateListingsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PrivateListingsDbContext>
{
    public PrivateListingsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__privatelistingsdb")
            ?? "Host=localhost;Port=5432;Database=privatelistings_design;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<PrivateListingsDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(PrivateListingsDbContext).Assembly.FullName))
            .Options;

        return new PrivateListingsDbContext(options);
    }
}
