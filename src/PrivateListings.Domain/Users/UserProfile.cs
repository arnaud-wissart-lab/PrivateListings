using PrivateListings.Domain.Common;
using PrivateListings.Domain.Privacy;

namespace PrivateListings.Domain.Users;

public sealed class UserProfile
{
    private readonly List<ConsentRecord> _consentHistory = [];

    private UserProfile(
        UserProfileId id,
        EmailAddress emailAddress,
        string displayName,
        DateTimeOffset createdAt,
        string? bio)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));

        Id = id;
        EmailAddress = emailAddress;
        DisplayName = Guard.AgainstBlank(displayName, nameof(displayName), 80);
        CreatedAt = Guard.AgainstDefault(createdAt, nameof(createdAt));
        Bio = Guard.OptionalText(bio, nameof(bio), 1_000);
    }

    public UserProfileId Id { get; }

    public EmailAddress? EmailAddress { get; private set; }

    public string DisplayName { get; private set; }

    public string? Bio { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? AnonymizedAt { get; private set; }

    public bool IsAnonymized => AnonymizedAt.HasValue;

    public IReadOnlyCollection<ConsentRecord> ConsentHistory => _consentHistory.AsReadOnly();

    public static UserProfile Create(
        EmailAddress emailAddress,
        string displayName,
        DateTimeOffset createdAt,
        string? bio = null)
        => new(UserProfileId.New(), emailAddress, displayName, createdAt, bio);

    public ConsentRecord RecordConsent(
        ConsentType type,
        bool isGranted,
        string policyVersion,
        string source,
        DateTimeOffset recordedAt)
    {
        EnsureNotAnonymized();

        var consentRecord = ConsentRecord.Create(Id, type, isGranted, policyVersion, source, recordedAt);
        _consentHistory.Add(consentRecord);

        return consentRecord;
    }

    public void Anonymize(DateTimeOffset anonymizedAt)
    {
        if (IsAnonymized)
        {
            return;
        }

        AnonymizedAt = Guard.AgainstDefault(anonymizedAt, nameof(anonymizedAt));
        EmailAddress = null;
        DisplayName = "Utilisateur anonymisé";
        Bio = null;
    }

    private void EnsureNotAnonymized()
    {
        if (IsAnonymized)
        {
            throw new DomainException("Un profil anonymisé ne peut plus être modifié.");
        }
    }
}
