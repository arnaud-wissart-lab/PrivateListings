using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace PrivateListings.E2ETests.Support;

internal sealed class TestHostEnvironment(string environmentName, string contentRootPath) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "PrivateListings.E2ETests";

    public string ContentRootPath { get; set; } = contentRootPath;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
