using PrivateListings.Domain.Common;
using PrivateListings.Domain.Messaging;
using PrivateListings.Domain.Moderation;

namespace PrivateListings.Application.Messaging;

public interface IMessagingService
{
    Task<IReadOnlyList<ConversationSummary>> ListConversationsAsync(
        UserProfileId actorId,
        CancellationToken cancellationToken = default);

    Task<ConversationThread?> GetConversationAsync(
        ConversationId conversationId,
        UserProfileId actorId,
        CancellationToken cancellationToken = default);

    Task<MessagingOperationResult> StartListingConversationAsync(
        StartListingConversationCommand command,
        CancellationToken cancellationToken = default);

    Task<MessagingOperationResult> SendMessageAsync(
        SendConversationMessageCommand command,
        CancellationToken cancellationToken = default);

    Task<MessagingOperationResult> MarkReadAsync(
        ConversationParticipantCommand command,
        CancellationToken cancellationToken = default);

    Task<MessagingOperationResult> MarkUnreadAsync(
        ConversationParticipantCommand command,
        CancellationToken cancellationToken = default);

    Task<MessagingOperationResult> ReportConversationAsync(
        ReportConversationCommand command,
        CancellationToken cancellationToken = default);

    Task<MessagingOperationResult> ReportMessageAsync(
        ReportMessageCommand command,
        CancellationToken cancellationToken = default);

    Task<MessagingOperationResult> BlockUserAsync(
        BlockUserCommand command,
        CancellationToken cancellationToken = default);
}

public interface IMessagingRepository
{
    Task<ListingConversationContext?> GetListingConversationContextAsync(
        ListingId listingId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<Conversation?> FindListingConversationForUpdateAsync(
        ListingId listingId,
        UserProfileId actorId,
        UserProfileId listingOwnerId,
        CancellationToken cancellationToken);

    Task<Conversation?> GetConversationForUpdateAsync(
        ConversationId conversationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Conversation>> ListConversationsForParticipantAsync(
        UserProfileId actorId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<UserProfileId, MessagingUserProfile>> GetUserProfilesByIdAsync(
        IReadOnlyCollection<UserProfileId> userProfileIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<ListingId, ListingMessagingInfo>> GetListingInfosByIdAsync(
        IReadOnlyCollection<ListingId> listingIds,
        CancellationToken cancellationToken);

    Task<UserBlock?> GetActiveBlockBetweenAsync(
        UserProfileId firstUserId,
        UserProfileId secondUserId,
        CancellationToken cancellationToken);

    Task<UserBlock?> GetUserBlockAsync(
        UserProfileId blockerId,
        UserProfileId blockedId,
        CancellationToken cancellationToken);

    Task<bool> HasRecentDuplicateMessageAsync(
        ConversationId conversationId,
        UserProfileId senderId,
        string body,
        DateTimeOffset since,
        CancellationToken cancellationToken);

    Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken);

    Task AddReportAsync(Report report, CancellationToken cancellationToken);

    Task AddUserBlockAsync(UserBlock userBlock, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IMessagingRateLimiter
{
    Task<MessagingRateLimitLease> TryAcquireAsync(
        MessagingRateLimitRequest request,
        CancellationToken cancellationToken = default);
}

public interface IMessageNotificationSender
{
    Task SendNewMessageNotificationAsync(
        MessageNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record ListingConversationContext(
    ListingId ListingId,
    UserProfileId OwnerId,
    string Title);

public sealed record ListingMessagingInfo(
    ListingId Id,
    string Title);

public sealed record MessagingUserProfile(
    UserProfileId Id,
    string DisplayName,
    string? EmailAddress);

public sealed record MessagingRateLimitRequest(
    UserProfileId ActorId,
    MessagingRequestContext RequestContext);

public sealed record MessagingRateLimitLease(bool IsAllowed, TimeSpan? RetryAfter = null);

public sealed record MessageNotification(
    string RecipientEmailAddress,
    ConversationId ConversationId,
    string ConversationUrl);
