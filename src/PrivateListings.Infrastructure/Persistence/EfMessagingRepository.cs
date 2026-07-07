using Microsoft.EntityFrameworkCore;
using PrivateListings.Application.Messaging;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;
using PrivateListings.Domain.Messaging;
using PrivateListings.Domain.Moderation;

namespace PrivateListings.Infrastructure.Persistence;

public sealed class EfMessagingRepository(PrivateListingsDbContext dbContext) : IMessagingRepository
{
    public Task<ListingConversationContext?> GetListingConversationContextAsync(
        ListingId listingId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => dbContext.Listings
            .AsNoTracking()
            .Where(listing => listing.Id == listingId)
            .Where(listing => listing.Status == ListingStatus.Published)
            .Where(listing => !listing.ExpiresAt.HasValue || listing.ExpiresAt.Value > now)
            .Select(listing => new ListingConversationContext(listing.Id, listing.OwnerId, listing.Title))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Conversation?> FindListingConversationForUpdateAsync(
        ListingId listingId,
        UserProfileId actorId,
        UserProfileId listingOwnerId,
        CancellationToken cancellationToken)
    {
        var conversations = await dbContext.Conversations
            .Include(conversation => conversation.Messages)
            .Include(conversation => conversation.ReadStates)
            .Where(conversation => conversation.ListingId == listingId)
            .OrderBy(conversation => conversation.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return conversations.FirstOrDefault(conversation =>
            conversation.ParticipantIds.Contains(actorId)
            && conversation.ParticipantIds.Contains(listingOwnerId));
    }

    public Task<Conversation?> GetConversationForUpdateAsync(
        ConversationId conversationId,
        CancellationToken cancellationToken)
        => dbContext.Conversations
            .Include(conversation => conversation.Messages)
            .Include(conversation => conversation.ReadStates)
            .SingleOrDefaultAsync(conversation => conversation.Id == conversationId, cancellationToken);

    public async Task<IReadOnlyList<Conversation>> ListConversationsForParticipantAsync(
        UserProfileId actorId,
        CancellationToken cancellationToken)
    {
        var conversationIds = await dbContext.ConversationReadStates
            .AsNoTracking()
            .Where(readState => readState.UserProfileId == actorId)
            .Select(readState => readState.ConversationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (conversationIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Messages)
            .Include(conversation => conversation.ReadStates)
            .Where(conversation => conversationIds.Contains(conversation.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<UserProfileId, MessagingUserProfile>> GetUserProfilesByIdAsync(
        IReadOnlyCollection<UserProfileId> userProfileIds,
        CancellationToken cancellationToken)
    {
        if (userProfileIds.Count == 0)
        {
            return new Dictionary<UserProfileId, MessagingUserProfile>();
        }

        var profiles = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(userProfile => userProfileIds.Contains(userProfile.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return profiles.ToDictionary(
            userProfile => userProfile.Id,
            userProfile => new MessagingUserProfile(
                userProfile.Id,
                userProfile.DisplayName,
                userProfile.EmailAddress.HasValue ? userProfile.EmailAddress.Value.Value : null));
    }

    public async Task<IReadOnlyDictionary<ListingId, ListingMessagingInfo>> GetListingInfosByIdAsync(
        IReadOnlyCollection<ListingId> listingIds,
        CancellationToken cancellationToken)
    {
        if (listingIds.Count == 0)
        {
            return new Dictionary<ListingId, ListingMessagingInfo>();
        }

        return await dbContext.Listings
            .AsNoTracking()
            .Where(listing => listingIds.Contains(listing.Id))
            .Select(listing => new ListingMessagingInfo(listing.Id, listing.Title))
            .ToDictionaryAsync(listing => listing.Id, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<UserBlock?> GetActiveBlockBetweenAsync(
        UserProfileId firstUserId,
        UserProfileId secondUserId,
        CancellationToken cancellationToken)
        => dbContext.UserBlocks
            .AsNoTracking()
            .Where(userBlock => userBlock.RevokedAt == null)
            .Where(userBlock =>
                userBlock.BlockerId == firstUserId && userBlock.BlockedId == secondUserId
                || userBlock.BlockerId == secondUserId && userBlock.BlockedId == firstUserId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<UserBlock?> GetUserBlockAsync(
        UserProfileId blockerId,
        UserProfileId blockedId,
        CancellationToken cancellationToken)
        => dbContext.UserBlocks
            .SingleOrDefaultAsync(
                userBlock => userBlock.BlockerId == blockerId && userBlock.BlockedId == blockedId,
                cancellationToken);

    public Task<bool> HasRecentDuplicateMessageAsync(
        ConversationId conversationId,
        UserProfileId senderId,
        string body,
        DateTimeOffset since,
        CancellationToken cancellationToken)
        => dbContext.Messages
            .AsNoTracking()
            .AnyAsync(
                message => message.ConversationId == conversationId
                    && message.SenderId == senderId
                    && !message.DeletedAt.HasValue
                    && message.SentAt >= since
                    && message.Body == body,
                cancellationToken);

    public async Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken)
        => await dbContext.Conversations.AddAsync(conversation, cancellationToken).ConfigureAwait(false);

    public async Task AddReportAsync(Report report, CancellationToken cancellationToken)
        => await dbContext.Reports.AddAsync(report, cancellationToken).ConfigureAwait(false);

    public async Task AddUserBlockAsync(UserBlock userBlock, CancellationToken cancellationToken)
        => await dbContext.UserBlocks.AddAsync(userBlock, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
