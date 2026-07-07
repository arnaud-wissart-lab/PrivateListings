using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Auditing;

public sealed class AuditLog
{
    private AuditLog()
    {
        Action = string.Empty;
    }

    private AuditLog(
        AuditLogId id,
        UserProfileId? actorUserProfileId,
        string action,
        AuditLogSubject subject,
        DateTimeOffset occurredAt,
        string? metadata)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));
        Guard.AgainstBlank(subject.Type, nameof(subject), 80);

        if (actorUserProfileId.HasValue)
        {
            Guard.AgainstEmpty(actorUserProfileId.Value.Value, nameof(actorUserProfileId));
        }

        Id = id;
        ActorUserProfileId = actorUserProfileId;
        Action = Guard.AgainstBlank(action, nameof(action), 120);
        Subject = subject;
        OccurredAt = Guard.AgainstDefault(occurredAt, nameof(occurredAt));
        Metadata = Guard.OptionalText(metadata, nameof(metadata), 4_000);
    }

    public AuditLogId Id { get; }

    public UserProfileId? ActorUserProfileId { get; }

    public string Action { get; }

    public AuditLogSubject Subject { get; }

    public DateTimeOffset OccurredAt { get; }

    public string? Metadata { get; }

    public static AuditLog Create(
        UserProfileId? actorUserProfileId,
        string action,
        AuditLogSubject subject,
        DateTimeOffset occurredAt,
        string? metadata = null)
        => new(AuditLogId.New(), actorUserProfileId, action, subject, occurredAt, metadata);
}
