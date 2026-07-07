using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Recommendations;

public sealed class Recommendation
{
    private Recommendation()
    {
    }

    private Recommendation(
        RecommendationId id,
        UserProfileId userProfileId,
        ListingId listingId,
        decimal score,
        string? reason,
        DateTimeOffset createdAt)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));
        Guard.AgainstEmpty(userProfileId.Value, nameof(userProfileId));
        Guard.AgainstEmpty(listingId.Value, nameof(listingId));

        if (score is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Le score doit être compris entre 0 et 1.");
        }

        Id = id;
        UserProfileId = userProfileId;
        ListingId = listingId;
        Score = score;
        Reason = Guard.OptionalText(reason, nameof(reason), 500);
        CreatedAt = Guard.AgainstDefault(createdAt, nameof(createdAt));
    }

    public RecommendationId Id { get; }

    public UserProfileId UserProfileId { get; }

    public ListingId ListingId { get; }

    public decimal Score { get; }

    public string? Reason { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? DismissedAt { get; private set; }

    public bool IsDismissed => DismissedAt.HasValue;

    public static Recommendation Create(
        UserProfileId userProfileId,
        ListingId listingId,
        decimal score,
        DateTimeOffset createdAt,
        string? reason = null)
        => new(RecommendationId.New(), userProfileId, listingId, score, reason, createdAt);

    public void Dismiss(DateTimeOffset dismissedAt)
    {
        if (IsDismissed)
        {
            return;
        }

        DismissedAt = Guard.AgainstDefault(dismissedAt, nameof(dismissedAt));
    }
}
