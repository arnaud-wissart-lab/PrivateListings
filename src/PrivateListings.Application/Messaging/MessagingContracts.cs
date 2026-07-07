using PrivateListings.Domain.Common;

namespace PrivateListings.Application.Messaging;

public sealed record ConversationSummary(
    ConversationId Id,
    ListingId? ListingId,
    string? ListingTitle,
    UserProfileId OtherParticipantId,
    string OtherParticipantDisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    bool IsUnread,
    bool IsBlocked);

public sealed record ConversationThread(
    ConversationId Id,
    ListingId? ListingId,
    string? ListingTitle,
    UserProfileId OtherParticipantId,
    string OtherParticipantDisplayName,
    DateTimeOffset CreatedAt,
    bool IsUnread,
    bool IsBlocked,
    IReadOnlyList<ConversationMessageItem> Messages);

public sealed record ConversationMessageItem(
    MessageId Id,
    UserProfileId SenderId,
    string SenderDisplayName,
    string Body,
    DateTimeOffset SentAt,
    bool IsFromCurrentUser,
    bool IsDeleted);

public sealed record StartListingConversationCommand(
    UserProfileId ActorId,
    ListingId ListingId,
    string Body,
    MessagingRequestContext RequestContext);

public sealed record SendConversationMessageCommand(
    UserProfileId ActorId,
    ConversationId ConversationId,
    string Body,
    MessagingRequestContext RequestContext);

public sealed record ConversationParticipantCommand(
    UserProfileId ActorId,
    ConversationId ConversationId);

public sealed record ReportConversationCommand(
    UserProfileId ActorId,
    ConversationId ConversationId,
    string Reason,
    string? Details);

public sealed record ReportMessageCommand(
    UserProfileId ActorId,
    ConversationId ConversationId,
    MessageId MessageId,
    string Reason,
    string? Details);

public sealed record BlockUserCommand(
    UserProfileId ActorId,
    ConversationId ConversationId,
    UserProfileId BlockedUserId);

public sealed record MessagingRequestContext(
    string? RemoteIpAddress,
    string? UserAgent,
    string? ApplicationBaseUrl);

public enum MessagingOperationStatus
{
    Success,
    NotFound,
    Forbidden,
    ValidationFailed,
    RateLimited,
    Blocked
}

public sealed record MessagingOperationResult(
    MessagingOperationStatus Status,
    ConversationId? ConversationId = null,
    MessageId? MessageId = null,
    IReadOnlyList<string>? Errors = null,
    TimeSpan? RetryAfter = null)
{
    public bool Succeeded => Status == MessagingOperationStatus.Success;

    public static MessagingOperationResult Success(
        ConversationId? conversationId = null,
        MessageId? messageId = null)
        => new(MessagingOperationStatus.Success, conversationId, messageId);

    public static MessagingOperationResult NotFound()
        => new(MessagingOperationStatus.NotFound, Errors: ["Conversation ou annonce introuvable."]);

    public static MessagingOperationResult Forbidden()
        => new(MessagingOperationStatus.Forbidden, Errors: ["Vous n'êtes pas autorisé à accéder à cette conversation."]);

    public static MessagingOperationResult ValidationFailed(params string[] errors)
        => new(MessagingOperationStatus.ValidationFailed, Errors: errors);

    public static MessagingOperationResult RateLimited(TimeSpan? retryAfter)
        => new(MessagingOperationStatus.RateLimited, Errors: ["Trop de messages envoyés. Réessayez plus tard."], RetryAfter: retryAfter);

    public static MessagingOperationResult Blocked()
        => new(MessagingOperationStatus.Blocked, Errors: ["L'envoi est impossible car un blocage existe entre les participants."]);
}
