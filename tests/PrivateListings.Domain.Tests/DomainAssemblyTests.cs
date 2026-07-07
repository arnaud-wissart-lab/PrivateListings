using PrivateListings.Domain;

namespace PrivateListings.Domain.Tests;

public sealed class DomainAssemblyTests
{
    [Fact]
    public void DomainAssemblyDoesNotReferenceInfrastructureOrWebConcerns()
    {
        var referencedAssemblies = AssemblyReference.Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        Assert.DoesNotContain("PrivateListings.Application", referencedAssemblies);
        Assert.DoesNotContain("PrivateListings.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", referencedAssemblies);
        Assert.DoesNotContain("Microsoft.AspNetCore", referencedAssemblies);
    }
}
