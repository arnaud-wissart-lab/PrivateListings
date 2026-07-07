using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PrivateListings.Infrastructure.Configuration;
using PrivateListings.Web.PublicUrls;

namespace PrivateListings.IntegrationTests;

public sealed class ProductionConfigurationValidationTests
{
    [Fact]
    public void ValidateAccepteConfigurationProductionDurcie()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PrivateListings:PublicBaseUrl"] = "https://privatelistings.example.test",
            ["PrivateListings:Email:SmtpHost"] = "smtp.example.test",
            ["PrivateListings:DemoSeed:Enabled"] = "false",
            ["AllowedHosts"] = "privatelistings.example.test"
        });

        PrivateListingsProductionConfigurationValidator.Validate(
            configuration,
            new TestHostEnvironment(Environments.Production),
            validateAllowedHosts: true);
    }

    [Fact]
    public void ValidateRefuseAllowedHostsWildcardEnProduction()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PrivateListings:PublicBaseUrl"] = "https://privatelistings.example.test",
            ["PrivateListings:Email:SmtpHost"] = "smtp.example.test",
            ["PrivateListings:DemoSeed:Enabled"] = "false",
            ["AllowedHosts"] = "*"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PrivateListingsProductionConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production),
                validateAllowedHosts: true));

        Assert.Contains("AllowedHosts", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateRefuseDemoSeedEnProduction()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PrivateListings:PublicBaseUrl"] = "https://privatelistings.example.test",
            ["PrivateListings:Email:SmtpHost"] = "smtp.example.test",
            ["PrivateListings:DemoSeed:Enabled"] = "true",
            ["AllowedHosts"] = "privatelistings.example.test"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PrivateListingsProductionConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production),
                validateAllowedHosts: true));

        Assert.Contains("DemoSeed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateRefuseSmtpManquantEnProduction()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PrivateListings:PublicBaseUrl"] = "https://privatelistings.example.test",
            ["PrivateListings:DemoSeed:Enabled"] = "false",
            ["AllowedHosts"] = "privatelistings.example.test"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PrivateListingsProductionConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production),
                validateAllowedHosts: true));

        Assert.Contains("SmtpHost", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateRefusePublicBaseUrlManquanteEnProduction()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PrivateListings:Email:SmtpHost"] = "smtp.example.test",
            ["PrivateListings:DemoSeed:Enabled"] = "false",
            ["AllowedHosts"] = "privatelistings.example.test"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PrivateListingsProductionConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production),
                validateAllowedHosts: true));

        Assert.Contains("PublicBaseUrl", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicUrlBuilderConstruitUrlDepuisBaseConfiguree()
    {
        var builder = new PublicUrlBuilder(Options.Create(new PrivateListingsPublicUrlOptions
        {
            PublicBaseUrl = "https://privatelistings.example.test/app/"
        }));

        var url = builder.BuildAbsoluteUrl("/confirm-email");

        Assert.Equal("https://privatelistings.example.test/app/confirm-email", url);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
