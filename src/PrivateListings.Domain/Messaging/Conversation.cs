using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Messaging;

public sealed class Conversation
{
    private readonly List<UserProfileId> _participantIds = [];
    private readonly List<Message> _messages = [];

    private Conversation()
    {
    }

    private Conversation(
        ConversationId id,
        ListingId? listingId,
        UserProfileId initiatorId,
        UserProfileId recipientId,
        DateTimeOffset createdAt)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));
        Guard.AgainstEmpty(initiatorId.Value, nameof(initiatorId));
        Guard.AgainstEmpty(recipientId.Value, nameof(recipientId));

        if (listingId.HasValue)
        {
            Guard.AgainstEmpty(listingId.Value.Value, nameof(listingId));
        }

        if (initiatorId == recipientId)
        {
            throw new ArgumentException("Une conversation doit avoir au moins deux participants distincts.", nameof(recipientId));
        }

        Id = id;
        ListingId = listingId;
        CreatedAt = Guard.AgainstDefault(createdAt, nameof(createdAt));
        _participantIds.Add(initiatorId);
        _participantIds.Add(recipientId);
    }

    public ConversationId Id { get; }

    public ListingId? ListingId { get; }

    public bool IsLinkedToListing => ListingId.HasValue;

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public bool IsClosed => ClosedAt.HasValue;

    public IReadOnlyCollection<UserProfileId> ParticipantIds => _participantIds.AsReadOnly();

    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    public static Conversation StartForListing(
        ListingId listingId,
        UserProfileId initiatorId,
        UserProfileId listingOwnerId,
        DateTimeOffset createdAt)
        => new(ConversationId.New(), listingId, initiatorId, listingOwnerId, createdAt);

    public static Conversation StartDirect(
        UserProfileId initiatorId,
        UserProfileId recipientId,
        DateTimeOffset createdAt)
        => new(ConversationId.New(), null, initiatorId, recipientId, createdAt);

    public Message AddMessage(UserProfileId senderId, string body, DateTimeOffset sentAt)
    {
        EnsureOpen();
        Guard.AgainstEmpty(senderId.Value, nameof(senderId));

        if (!_participantIds.Contains(senderId))
        {
            throw new DomainException("Seul un participant peut envoyer un message dans cette conversation.");
        }

        var message = Message.Create(Id, senderId, body, sentAt);
        _messages.Add(message);

        return message;
    }

    public void AddParticipant(UserProfileId participantId)
    {
        EnsureOpen();
        Guard.AgainstEmpty(participantId.Value, nameof(participantId));

        if (!_participantIds.Contains(participantId))
        {
            _participantIds.Add(participantId);
        }
    }

    public void Close(DateTimeOffset closedAt)
    {
        if (IsClosed)
        {
            return;
        }

        ClosedAt = Guard.AgainstDefault(closedAt, nameof(closedAt));
    }

    private void EnsureOpen()
    {
        if (IsClosed)
        {
            throw new DomainException("Une conversation clôturée ne peut plus être modifiée.");
        }
    }
}
