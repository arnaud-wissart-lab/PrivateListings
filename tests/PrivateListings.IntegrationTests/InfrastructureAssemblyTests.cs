using Microsoft.EntityFrameworkCore;
using PrivateListings.Infrastructure.Persistence;

namespace PrivateListings.IntegrationTests;

public sealed class InfrastructureAssemblyTests
{
    [Fact]
    public void InfrastructureDbContextIsConstructibleForIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<PrivateListingsDbContext>().Options;

        using var dbContext = new PrivateListingsDbContext(options);

        Assert.Equal(nameof(PrivateListingsDbContext), dbContext.GetType().Name);
    }
}
