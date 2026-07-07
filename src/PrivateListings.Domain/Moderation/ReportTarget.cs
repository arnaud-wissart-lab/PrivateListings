using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Moderation;

public readonly record struct ReportTarget
{
    private ReportTarget(ReportTargetType type, Guid targetId)
    {
        Guard.AgainstEmpty(targetId, nameof(targetId));

        Type = type;
        TargetId = targetId;
    }

    public ReportTargetType Type { get; }

    public Guid TargetId { get; }

    public static ReportTarget ForListing(ListingId listingId)
    {
        Guard.AgainstEmpty(listingId.Value, nameof(listingId));
        return new ReportTarget(ReportTargetType.Listing, listingId.Value);
    }

    public static ReportTarget ForMessage(MessageId messageId)
    {
        Guard.AgainstEmpty(messageId.Value, nameof(messageId));
        return new ReportTarget(ReportTargetType.Message, messageId.Value);
    }

    public static ReportTarget ForUserProfile(UserProfileId userProfileId)
    {
        Guard.AgainstEmpty(userProfileId.Value, nameof(userProfileId));
        return new ReportTarget(ReportTargetType.UserProfile, userProfileId.Value);
    }
}
