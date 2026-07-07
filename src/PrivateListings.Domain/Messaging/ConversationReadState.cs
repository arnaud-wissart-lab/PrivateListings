using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Messaging;

public sealed class ConversationReadState
{
    private ConversationReadState()
    {
    }

    private ConversationReadState(
        ConversationId conversationId,
        UserProfileId userProfileId,
        DateTimeOffset createdAt)
    {
        Guard.AgainstEmpty(conversationId.Value, nameof(conversationId));
        Guard.AgainstEmpty(userProfileId.Value, nameof(userProfileId));

        ConversationId = conversationId;
        UserProfileId = userProfileId;
        CreatedAt = Guard.AgainstDefault(createdAt, nameof(createdAt));
    }

    public ConversationId ConversationId { get; }

    public UserProfileId UserProfileId { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? LastReadAt { get; private set; }

    public bool IsMarkedUnread { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static ConversationReadState Create(
        ConversationId conversationId,
        UserProfileId userProfileId,
        DateTimeOffset createdAt)
        => new(conversationId, userProfileId, createdAt);

    public void MarkRead(DateTimeOffset readAt)
    {
        LastReadAt = Guard.AgainstDefault(readAt, nameof(readAt));
        IsMarkedUnread = false;
        UpdatedAt = readAt;
    }

    public void MarkUnread(DateTimeOffset markedAt)
    {
        IsMarkedUnread = true;
        UpdatedAt = Guard.AgainstDefault(markedAt, nameof(markedAt));
    }
}
