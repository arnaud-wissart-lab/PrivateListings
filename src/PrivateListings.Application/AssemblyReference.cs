using System.Reflection;

namespace PrivateListings.Application;

public static class AssemblyReference
{
    public static Assembly Assembly => typeof(AssemblyReference).Assembly;

    public static Assembly DomainAssembly => Domain.AssemblyReference.Assembly;
}
