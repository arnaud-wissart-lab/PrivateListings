using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Messaging;

public sealed class Message
{
    private Message(
        MessageId id,
        ConversationId conversationId,
        UserProfileId senderId,
        string body,
        DateTimeOffset sentAt)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));
        Guard.AgainstEmpty(conversationId.Value, nameof(conversationId));
        Guard.AgainstEmpty(senderId.Value, nameof(senderId));

        Id = id;
        ConversationId = conversationId;
        SenderId = senderId;
        Body = Guard.AgainstBlank(body, nameof(body), 4_000);
        SentAt = Guard.AgainstDefault(sentAt, nameof(sentAt));
    }

    public MessageId Id { get; }

    public ConversationId ConversationId { get; }

    public UserProfileId SenderId { get; }

    public string Body { get; private set; }

    public DateTimeOffset SentAt { get; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public static Message Create(
        ConversationId conversationId,
        UserProfileId senderId,
        string body,
        DateTimeOffset sentAt)
        => new(MessageId.New(), conversationId, senderId, body, sentAt);

    public void Delete(DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            return;
        }

        DeletedAt = Guard.AgainstDefault(deletedAt, nameof(deletedAt));
        Body = string.Empty;
    }
}
