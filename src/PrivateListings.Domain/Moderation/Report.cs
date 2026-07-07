using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Moderation;

public sealed class Report
{
    private Report(
        ReportId id,
        UserProfileId reporterId,
        ReportTarget target,
        string reason,
        string? details,
        DateTimeOffset createdAt)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));
        Guard.AgainstEmpty(reporterId.Value, nameof(reporterId));
        Guard.AgainstEmpty(target.TargetId, nameof(target));

        Id = id;
        ReporterId = reporterId;
        Target = target;
        Reason = Guard.AgainstBlank(reason, nameof(reason), 160);
        Details = Guard.OptionalText(details, nameof(details), 2_000);
        CreatedAt = Guard.AgainstDefault(createdAt, nameof(createdAt));
        Status = ReportStatus.Open;
    }

    public ReportId Id { get; }

    public UserProfileId ReporterId { get; }

    public ReportTarget Target { get; }

    public string Reason { get; }

    public string? Details { get; }

    public ReportStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public UserProfileId? ReviewedById { get; private set; }

    public string? ResolutionNote { get; private set; }

    public static Report Create(
        UserProfileId reporterId,
        ReportTarget target,
        string reason,
        DateTimeOffset createdAt,
        string? details = null)
        => new(ReportId.New(), reporterId, target, reason, details, createdAt);

    public void MarkUnderReview(UserProfileId reviewerId, DateTimeOffset reviewedAt)
    {
        Guard.AgainstEmpty(reviewerId.Value, nameof(reviewerId));

        if (Status != ReportStatus.Open)
        {
            throw new DomainException("Seul un signalement ouvert peut passer en revue.");
        }

        Status = ReportStatus.UnderReview;
        ReviewedById = reviewerId;
        ReviewedAt = Guard.AgainstDefault(reviewedAt, nameof(reviewedAt));
    }

    public void Resolve(UserProfileId reviewerId, string note, DateTimeOffset resolvedAt)
        => Close(ReportStatus.Resolved, reviewerId, note, resolvedAt);

    public void Reject(UserProfileId reviewerId, string note, DateTimeOffset rejectedAt)
        => Close(ReportStatus.Rejected, reviewerId, note, rejectedAt);

    private void Close(ReportStatus status, UserProfileId reviewerId, string note, DateTimeOffset reviewedAt)
    {
        Guard.AgainstEmpty(reviewerId.Value, nameof(reviewerId));

        if (Status is ReportStatus.Resolved or ReportStatus.Rejected)
        {
            throw new DomainException("Un signalement déjà clôturé ne peut plus être modifié.");
        }

        Status = status;
        ReviewedById = reviewerId;
        ReviewedAt = Guard.AgainstDefault(reviewedAt, nameof(reviewedAt));
        ResolutionNote = Guard.AgainstBlank(note, nameof(note), 1_000);
    }
}
