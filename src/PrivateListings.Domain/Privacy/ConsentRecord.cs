using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Privacy;

public sealed class ConsentRecord
{
    private ConsentRecord(
        ConsentRecordId id,
        UserProfileId userProfileId,
        ConsentType type,
        bool isGranted,
        string policyVersion,
        string source,
        DateTimeOffset recordedAt)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));
        Guard.AgainstEmpty(userProfileId.Value, nameof(userProfileId));

        Id = id;
        UserProfileId = userProfileId;
        Type = type;
        IsGranted = isGranted;
        PolicyVersion = Guard.AgainstBlank(policyVersion, nameof(policyVersion), 64);
        Source = Guard.AgainstBlank(source, nameof(source), 128);
        RecordedAt = Guard.AgainstDefault(recordedAt, nameof(recordedAt));
    }

    public ConsentRecordId Id { get; }

    public UserProfileId UserProfileId { get; }

    public ConsentType Type { get; }

    public bool IsGranted { get; }

    public string PolicyVersion { get; }

    public string Source { get; }

    public DateTimeOffset RecordedAt { get; }

    public static ConsentRecord Create(
        UserProfileId userProfileId,
        ConsentType type,
        bool isGranted,
        string policyVersion,
        string source,
        DateTimeOffset recordedAt)
        => new(
            ConsentRecordId.New(),
            userProfileId,
            type,
            isGranted,
            policyVersion,
            source,
            recordedAt);
}
