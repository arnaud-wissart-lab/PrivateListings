using Microsoft.Extensions.Options;
using PrivateListings.Application.Authentication;

namespace PrivateListings.Application.Tests;

public sealed class AccountPasswordPolicyTests
{
    [Fact]
    public void ValidateAccepteMotDePasseLongSansReglesArtificielles()
    {
        var policy = new AccountPasswordPolicy(Options.Create(new AccountPasswordPolicyOptions()));

        var result = policy.Validate("phrase de passe longue et compatible gestionnaire");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateRefuseMotDePasseTropCourt()
    {
        var policy = new AccountPasswordPolicy(Options.Create(new AccountPasswordPolicyOptions()));

        var result = policy.Validate("court");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("au moins 12", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateRefuseMotDePasseTropLongSansTroncature()
    {
        var policy = new AccountPasswordPolicy(Options.Create(new AccountPasswordPolicyOptions
        {
            MaximumLength = 64
        }));

        var result = policy.Validate(new string('a', 65));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("64", StringComparison.Ordinal));
    }
}
