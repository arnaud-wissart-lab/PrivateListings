using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Auditing;

public readonly record struct AuditLogSubject
{
    private AuditLogSubject(string type, Guid? entityId)
    {
        if (entityId.HasValue)
        {
            Guard.AgainstEmpty(entityId.Value, nameof(entityId));
        }

        Type = Guard.AgainstBlank(type, nameof(type), 80);
        EntityId = entityId;
    }

    public string Type { get; }

    public Guid? EntityId { get; }

    public static AuditLogSubject ForListing(ListingId listingId)
    {
        Guard.AgainstEmpty(listingId.Value, nameof(listingId));
        return new AuditLogSubject("Listing", listingId.Value);
    }

    public static AuditLogSubject ForListingCategory(ListingCategoryId listingCategoryId)
    {
        Guard.AgainstEmpty(listingCategoryId.Value, nameof(listingCategoryId));
        return new AuditLogSubject("ListingCategory", listingCategoryId.Value);
    }

    public static AuditLogSubject ForConversation(ConversationId conversationId)
    {
        Guard.AgainstEmpty(conversationId.Value, nameof(conversationId));
        return new AuditLogSubject("Conversation", conversationId.Value);
    }

    public static AuditLogSubject ForMessage(MessageId messageId)
    {
        Guard.AgainstEmpty(messageId.Value, nameof(messageId));
        return new AuditLogSubject("Message", messageId.Value);
    }

    public static AuditLogSubject ForUserProfile(UserProfileId userProfileId)
    {
        Guard.AgainstEmpty(userProfileId.Value, nameof(userProfileId));
        return new AuditLogSubject("UserProfile", userProfileId.Value);
    }

    public static AuditLogSubject ForReport(ReportId reportId)
    {
        Guard.AgainstEmpty(reportId.Value, nameof(reportId));
        return new AuditLogSubject("Report", reportId.Value);
    }

    public static AuditLogSubject ForSystem(string type)
        => new(type, null);
}
