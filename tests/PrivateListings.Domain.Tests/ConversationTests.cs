using PrivateListings.Domain.Common;
using PrivateListings.Domain.Messaging;

namespace PrivateListings.Domain.Tests;

public sealed class ConversationTests
{
    [Fact]
    public void StartForListingCreatesConversationLinkedToListing()
    {
        var listingId = ListingId.New();
        var initiatorId = UserProfileId.New();
        var listingOwnerId = UserProfileId.New();
        var createdAt = new DateTimeOffset(2026, 7, 7, 9, 0, 0, TimeSpan.Zero);

        var conversation = Conversation.StartForListing(listingId, initiatorId, listingOwnerId, createdAt);
        var message = conversation.AddMessage(initiatorId, "Bonjour, l'annonce est-elle toujours disponible ?", createdAt.AddMinutes(2));

        Assert.True(conversation.IsLinkedToListing);
        Assert.Equal(listingId, conversation.ListingId);
        Assert.Contains(initiatorId, conversation.ParticipantIds);
        Assert.Contains(listingOwnerId, conversation.ParticipantIds);
        Assert.Equal(conversation.Id, message.ConversationId);
    }

    [Fact]
    public void MarkReadAndUnreadUpdatesParticipantState()
    {
        var listingId = ListingId.New();
        var initiatorId = UserProfileId.New();
        var listingOwnerId = UserProfileId.New();
        var createdAt = new DateTimeOffset(2026, 7, 7, 9, 0, 0, TimeSpan.Zero);
        var conversation = Conversation.StartForListing(listingId, initiatorId, listingOwnerId, createdAt);

        conversation.AddMessage(listingOwnerId, "Bonjour, réponse fictive.", createdAt.AddMinutes(2));

        Assert.True(conversation.IsUnreadFor(initiatorId));

        conversation.MarkRead(initiatorId, createdAt.AddMinutes(3));

        Assert.False(conversation.IsUnreadFor(initiatorId));

        conversation.MarkUnread(initiatorId, createdAt.AddMinutes(4));

        Assert.True(conversation.IsUnreadFor(initiatorId));
    }
}
