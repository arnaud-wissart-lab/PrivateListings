using PrivateListings.Application;

namespace PrivateListings.Application.Tests;

public sealed class ApplicationAssemblyTests
{
    [Fact]
    public void ApplicationAssemblyReferencesDomainWithoutInfrastructure()
    {
        var referencedAssemblies = AssemblyReference.Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        Assert.Contains("PrivateListings.Domain", referencedAssemblies);
        Assert.DoesNotContain("PrivateListings.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("PrivateListings.Web", referencedAssemblies);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", referencedAssemblies);
    }
}
