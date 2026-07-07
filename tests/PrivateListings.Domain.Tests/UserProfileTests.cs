using PrivateListings.Domain.Privacy;
using PrivateListings.Domain.Users;

namespace PrivateListings.Domain.Tests;

public sealed class UserProfileTests
{
    [Fact]
    public void AnonymizeRemovesPersonalDataWhileKeepingProfileIdentity()
    {
        var createdAt = new DateTimeOffset(2026, 7, 7, 8, 0, 0, TimeSpan.Zero);
        var anonymizedAt = createdAt.AddDays(1);
        var profile = UserProfile.Create(
            EmailAddress.Create("marie@example.test"),
            "Marie",
            createdAt,
            "Je participe aux échanges du quartier.");
        var profileId = profile.Id;

        profile.Anonymize(anonymizedAt);

        Assert.Equal(profileId, profile.Id);
        Assert.True(profile.IsAnonymized);
        Assert.Null(profile.EmailAddress);
        Assert.Null(profile.Bio);
        Assert.Equal("Utilisateur anonymisé", profile.DisplayName);
        Assert.Equal(anonymizedAt, profile.AnonymizedAt);
    }

    [Fact]
    public void RecordConsentKeepsConsentHistory()
    {
        var createdAt = new DateTimeOffset(2026, 7, 7, 8, 0, 0, TimeSpan.Zero);
        var profile = UserProfile.Create(
            EmailAddress.Create("karim@example.test"),
            "Karim",
            createdAt);

        var firstConsent = profile.RecordConsent(
            ConsentType.PrivacyPolicy,
            isGranted: true,
            "2026.07",
            "signup",
            createdAt.AddMinutes(1));
        var secondConsent = profile.RecordConsent(
            ConsentType.PrivacyPolicy,
            isGranted: false,
            "2026.08",
            "account-settings",
            createdAt.AddMonths(1));

        Assert.Equal(2, profile.ConsentHistory.Count);
        Assert.Contains(firstConsent, profile.ConsentHistory);
        Assert.Contains(secondConsent, profile.ConsentHistory);
        Assert.NotEqual(firstConsent.Id, secondConsent.Id);
        Assert.False(secondConsent.IsGranted);
    }
}
