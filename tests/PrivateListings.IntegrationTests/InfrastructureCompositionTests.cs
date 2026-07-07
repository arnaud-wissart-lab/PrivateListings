using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PrivateListings.Application.Authentication;
using PrivateListings.Application.Listings;
using PrivateListings.Infrastructure.Identity;

namespace PrivateListings.IntegrationTests;

[Collection(PostgreSqlTestCollectionDefinition.Name)]
public sealed class InfrastructureCompositionTests(PostgreSqlFixture fixture)
{
    [Fact]
    public void WebInfrastructureConstruitLeProviderEtResoutIdentity()
    {
        using var serviceProvider = fixture.CreateServiceProvider(validateOnBuild: true);
        using var scope = serviceProvider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<UserManager<PrivateListingsUser>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RoleManager<PrivateListingsRole>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<SignInManager<PrivateListingsUser>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserAccountService>());
    }

    [Fact]
    public void WorkerInfrastructureResoutLesServicesNecessairesSansServicesWeb()
    {
        using var serviceProvider = fixture.CreateWorkerServiceProvider(validateOnBuild: true);
        using var scope = serviceProvider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IListingService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AccountSecurityTokenStore>());
        Assert.Throws<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<IUserAccountService>());
        Assert.Throws<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<SignInManager<PrivateListingsUser>>());
    }
}
