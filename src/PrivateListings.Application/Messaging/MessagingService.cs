using Microsoft.Extensions.Options;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Messaging;
using PrivateListings.Domain.Moderation;

namespace PrivateListings.Application.Messaging;

public sealed class MessagingService(
    IMessagingRepository repository,
    IMessagingRateLimiter rateLimiter,
    IMessageNotificationSender notificationSender,
    IOptions<MessagingSpamOptions> spamOptions,
    TimeProvider timeProvider)
    : IMessagingService
{
    private const int MaxMessageLength = 4_000;

    public async Task<IReadOnlyList<ConversationSummary>> ListConversationsAsync(
        UserProfileId actorId,
        CancellationToken cancellationToken = default)
    {
        var conversations = await repository
            .ListConversationsForParticipantAsync(actorId, cancellationToken)
            .ConfigureAwait(false);

        var profiles = await LoadParticipantProfilesAsync(conversations, cancellationToken).ConfigureAwait(false);
        var listings = await LoadListingInfosAsync(conversations, cancellationToken).ConfigureAwait(false);

        var summaries = new List<ConversationSummary>(conversations.Count);
        foreach (var conversation in conversations)
        {
            if (!TryGetOtherParticipant(conversation, actorId, out var otherParticipantId))
            {
                continue;
            }

            var block = await repository
                .GetActiveBlockBetweenAsync(actorId, otherParticipantId, cancellationToken)
                .ConfigureAwait(false);

            summaries.Add(new ConversationSummary(
                conversation.Id,
                conversation.ListingId,
                ResolveListingTitle(conversation, listings),
                otherParticipantId,
                ResolveDisplayName(otherParticipantId, profiles),
                conversation.CreatedAt,
                conversation.Messages
                    .Where(message => !message.IsDeleted)
                    .OrderByDescending(message => message.SentAt)
                    .Select(message => (DateTimeOffset?)message.SentAt)
                    .FirstOrDefault(),
                conversation.IsUnreadFor(actorId),
                block is not null));
        }

        return summaries
            .OrderByDescending(summary => summary.LastMessageAt ?? summary.CreatedAt)
            .ToList();
    }

    public async Task<ConversationThread?> GetConversationAsync(
        ConversationId conversationId,
        UserProfileId actorId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository
            .GetConversationForUpdateAsync(conversationId, cancellationToken)
            .ConfigureAwait(false);

        if (conversation is null || !IsParticipant(conversation, actorId))
        {
            return null;
        }

        var profiles = await LoadParticipantProfilesAsync([conversation], cancellationToken).ConfigureAwait(false);
        var listings = await LoadListingInfosAsync([conversation], cancellationToken).ConfigureAwait(false);

        if (!TryGetOtherParticipant(conversation, actorId, out var otherParticipantId))
        {
            return null;
        }

        var block = await repository
            .GetActiveBlockBetweenAsync(actorId, otherParticipantId, cancellationToken)
            .ConfigureAwait(false);

        return new ConversationThread(
            conversation.Id,
            conversation.ListingId,
            ResolveListingTitle(conversation, listings),
            otherParticipantId,
            ResolveDisplayName(otherParticipantId, profiles),
            conversation.CreatedAt,
            conversation.IsUnreadFor(actorId),
            block is not null,
            conversation.Messages
                .OrderBy(message => message.SentAt)
                .Select(message => new ConversationMessageItem(
                    message.Id,
                    message.SenderId,
                    ResolveDisplayName(message.SenderId, profiles),
                    message.Body,
                    message.SentAt,
                    message.SenderId == actorId,
                    message.IsDeleted))
                .ToList());
    }

    public async Task<MessagingOperationResult> StartListingConversationAsync(
        StartListingConversationCommand command,
        CancellationToken cancellationToken = default)
    {
        var bodyValidation = ValidateMessageBody(command.Body, out var body);
        if (bodyValidation is not null)
        {
            return bodyValidation;
        }

        var rateLimit = await CheckRateLimitAsync(
            command.ActorId,
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);

        if (rateLimit is not null)
        {
            return rateLimit;
        }

        var now = timeProvider.GetUtcNow();
        var listing = await repository
            .GetListingConversationContextAsync(command.ListingId, now, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return MessagingOperationResult.NotFound();
        }

        if (listing.OwnerId == command.ActorId)
        {
            return MessagingOperationResult.ValidationFailed("Vous ne pouvez pas répondre à votre propre annonce.");
        }

        if (await HasActiveBlockAsync(command.ActorId, [listing.OwnerId], cancellationToken).ConfigureAwait(false))
        {
            return MessagingOperationResult.Blocked();
        }

        var conversation = await repository
            .FindListingConversationForUpdateAsync(
                listing.ListingId,
                command.ActorId,
                listing.OwnerId,
                cancellationToken)
            .ConfigureAwait(false);

        if (conversation is null)
        {
            conversation = Conversation.StartForListing(listing.ListingId, command.ActorId, listing.OwnerId, now);
            await repository.AddConversationAsync(conversation, cancellationToken).ConfigureAwait(false);
        }

        var duplicate = await HasRecentDuplicateAsync(
            conversation.Id,
            command.ActorId,
            body,
            now,
            cancellationToken).ConfigureAwait(false);

        if (duplicate)
        {
            return MessagingOperationResult.ValidationFailed("Ce message semble déjà avoir été envoyé récemment.");
        }

        return await AddMessageAndNotifyAsync(
            conversation,
            command.ActorId,
            body,
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<MessagingOperationResult> SendMessageAsync(
        SendConversationMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        var bodyValidation = ValidateMessageBody(command.Body, out var body);
        if (bodyValidation is not null)
        {
            return bodyValidation;
        }

        var rateLimit = await CheckRateLimitAsync(
            command.ActorId,
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);

        if (rateLimit is not null)
        {
            return rateLimit;
        }

        var conversation = await repository
            .GetConversationForUpdateAsync(command.ConversationId, cancellationToken)
            .ConfigureAwait(false);

        if (conversation is null)
        {
            return MessagingOperationResult.NotFound();
        }

        if (!IsParticipant(conversation, command.ActorId))
        {
            return MessagingOperationResult.Forbidden();
        }

        var recipients = conversation.ParticipantIds
            .Where(participantId => participantId != command.ActorId)
            .ToArray();

        if (await HasActiveBlockAsync(command.ActorId, recipients, cancellationToken).ConfigureAwait(false))
        {
            return MessagingOperationResult.Blocked();
        }

        var now = timeProvider.GetUtcNow();
        var duplicate = await HasRecentDuplicateAsync(
            conversation.Id,
            command.ActorId,
            body,
            now,
            cancellationToken).ConfigureAwait(false);

        if (duplicate)
        {
            return MessagingOperationResult.ValidationFailed("Ce message semble déjà avoir été envoyé récemment.");
        }

        return await AddMessageAndNotifyAsync(
            conversation,
            command.ActorId,
            body,
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<MessagingOperationResult> MarkReadAsync(
        ConversationParticipantCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository
            .GetConversationForUpdateAsync(command.ConversationId, cancellationToken)
            .ConfigureAwait(false);

        if (conversation is null)
        {
            return MessagingOperationResult.NotFound();
        }

        if (!IsParticipant(conversation, command.ActorId))
        {
            return MessagingOperationResult.Forbidden();
        }

        conversation.MarkRead(command.ActorId, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MessagingOperationResult.Success(conversation.Id);
    }

    public async Task<MessagingOperationResult> MarkUnreadAsync(
        ConversationParticipantCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository
            .GetConversationForUpdateAsync(command.ConversationId, cancellationToken)
            .ConfigureAwait(false);

        if (conversation is null)
        {
            return MessagingOperationResult.NotFound();
        }

        if (!IsParticipant(conversation, command.ActorId))
        {
            return MessagingOperationResult.Forbidden();
        }

        conversation.MarkUnread(command.ActorId, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MessagingOperationResult.Success(conversation.Id);
    }

    public async Task<MessagingOperationResult> ReportConversationAsync(
        ReportConversationCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository
            .GetConversationForUpdateAsync(command.ConversationId, cancellationToken)
            .ConfigureAwait(false);

        if (conversation is null)
        {
            return MessagingOperationResult.NotFound();
        }

        if (!IsParticipant(conversation, command.ActorId))
        {
            return MessagingOperationResult.Forbidden();
        }

        return await AddReportAsync(
            command.ActorId,
            ReportTarget.ForConversation(conversation.Id),
            command.Reason,
            command.Details,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<MessagingOperationResult> ReportMessageAsync(
        ReportMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository
            .GetConversationForUpdateAsync(command.ConversationId, cancellationToken)
            .ConfigureAwait(false);

        if (conversation is null)
        {
            return MessagingOperationResult.NotFound();
        }

        if (!IsParticipant(conversation, command.ActorId))
        {
            return MessagingOperationResult.Forbidden();
        }

        if (!conversation.Messages.Any(message => message.Id == command.MessageId))
        {
            return MessagingOperationResult.NotFound();
        }

        return await AddReportAsync(
            command.ActorId,
            ReportTarget.ForMessage(command.MessageId),
            command.Reason,
            command.Details,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<MessagingOperationResult> BlockUserAsync(
        BlockUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository
            .GetConversationForUpdateAsync(command.ConversationId, cancellationToken)
            .ConfigureAwait(false);

        if (conversation is null)
        {
            return MessagingOperationResult.NotFound();
        }

        if (!IsParticipant(conversation, command.ActorId))
        {
            return MessagingOperationResult.Forbidden();
        }

        if (command.ActorId == command.BlockedUserId
            || !conversation.ParticipantIds.Contains(command.BlockedUserId))
        {
            return MessagingOperationResult.ValidationFailed("L'utilisateur à bloquer doit être un autre participant de la conversation.");
        }

        var existingBlock = await repository
            .GetUserBlockAsync(command.ActorId, command.BlockedUserId, cancellationToken)
            .ConfigureAwait(false);

        if (existingBlock is null)
        {
            var userBlock = UserBlock.Create(command.ActorId, command.BlockedUserId, timeProvider.GetUtcNow());
            await repository.AddUserBlockAsync(userBlock, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return MessagingOperationResult.Success(conversation.Id);
    }

    private async Task<MessagingOperationResult> AddMessageAndNotifyAsync(
        Conversation conversation,
        UserProfileId senderId,
        string body,
        MessagingRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = conversation.AddMessage(senderId, body, timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await NotifyRecipientsAsync(
                conversation,
                senderId,
                requestContext,
                cancellationToken).ConfigureAwait(false);

            return MessagingOperationResult.Success(conversation.Id, message.Id);
        }
        catch (ArgumentException exception)
        {
            return MessagingOperationResult.ValidationFailed(exception.Message);
        }
        catch (DomainException exception)
        {
            return MessagingOperationResult.ValidationFailed(exception.Message);
        }
    }

    private async Task NotifyRecipientsAsync(
        Conversation conversation,
        UserProfileId senderId,
        MessagingRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var recipientIds = conversation.ParticipantIds
            .Where(participantId => participantId != senderId)
            .ToArray();

        if (recipientIds.Length == 0)
        {
            return;
        }

        var profiles = await repository
            .GetUserProfilesByIdAsync(recipientIds, cancellationToken)
            .ConfigureAwait(false);

        var conversationUrl = BuildConversationUrl(requestContext.ApplicationBaseUrl, conversation.Id);
        foreach (var recipientId in recipientIds)
        {
            if (!profiles.TryGetValue(recipientId, out var profile)
                || string.IsNullOrWhiteSpace(profile.EmailAddress))
            {
                continue;
            }

            await notificationSender
                .SendNewMessageNotificationAsync(
                    new MessageNotification(profile.EmailAddress, conversation.Id, conversationUrl),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<MessagingOperationResult> AddReportAsync(
        UserProfileId reporterId,
        ReportTarget target,
        string reason,
        string? details,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = Report.Create(reporterId, target, reason, timeProvider.GetUtcNow(), details);
            await repository.AddReportAsync(report, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return MessagingOperationResult.Success();
        }
        catch (ArgumentException exception)
        {
            return MessagingOperationResult.ValidationFailed(exception.Message);
        }
    }

    private async Task<MessagingOperationResult?> CheckRateLimitAsync(
        UserProfileId actorId,
        MessagingRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var lease = await rateLimiter
            .TryAcquireAsync(new MessagingRateLimitRequest(actorId, requestContext), cancellationToken)
            .ConfigureAwait(false);

        return lease.IsAllowed ? null : MessagingOperationResult.RateLimited(lease.RetryAfter);
    }

    private async Task<bool> HasRecentDuplicateAsync(
        ConversationId conversationId,
        UserProfileId senderId,
        string body,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => await repository
            .HasRecentDuplicateMessageAsync(
                conversationId,
                senderId,
                body,
                now - spamOptions.Value.DuplicateMessageWindow,
                cancellationToken)
            .ConfigureAwait(false);

    private async Task<bool> HasActiveBlockAsync(
        UserProfileId actorId,
        IReadOnlyCollection<UserProfileId> otherParticipantIds,
        CancellationToken cancellationToken)
    {
        foreach (var otherParticipantId in otherParticipantIds)
        {
            var block = await repository
                .GetActiveBlockBetweenAsync(actorId, otherParticipantId, cancellationToken)
                .ConfigureAwait(false);

            if (block is not null)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<IReadOnlyDictionary<UserProfileId, MessagingUserProfile>> LoadParticipantProfilesAsync(
        IReadOnlyCollection<Conversation> conversations,
        CancellationToken cancellationToken)
    {
        var participantIds = conversations
            .SelectMany(conversation => conversation.ParticipantIds)
            .Distinct()
            .ToArray();

        return await repository
            .GetUserProfilesByIdAsync(participantIds, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<ListingId, ListingMessagingInfo>> LoadListingInfosAsync(
        IReadOnlyCollection<Conversation> conversations,
        CancellationToken cancellationToken)
    {
        var listingIds = conversations
            .Select(conversation => conversation.ListingId)
            .Where(listingId => listingId.HasValue)
            .Select(listingId => listingId!.Value)
            .Distinct()
            .ToArray();

        return await repository
            .GetListingInfosByIdAsync(listingIds, cancellationToken)
            .ConfigureAwait(false);
    }

    private static MessagingOperationResult? ValidateMessageBody(string body, out string normalizedBody)
    {
        normalizedBody = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();

        if (normalizedBody.Length == 0)
        {
            return MessagingOperationResult.ValidationFailed("Le message ne peut pas être vide.");
        }

        if (normalizedBody.Length > MaxMessageLength)
        {
            return MessagingOperationResult.ValidationFailed($"Le message ne peut pas dépasser {MaxMessageLength} caractères.");
        }

        return null;
    }

    private static bool IsParticipant(Conversation conversation, UserProfileId actorId)
        => conversation.ParticipantIds.Contains(actorId);

    private static bool TryGetOtherParticipant(
        Conversation conversation,
        UserProfileId actorId,
        out UserProfileId otherParticipantId)
    {
        otherParticipantId = conversation.ParticipantIds.FirstOrDefault(participantId => participantId != actorId);
        return otherParticipantId.Value != Guid.Empty;
    }

    private static string ResolveDisplayName(
        UserProfileId userProfileId,
        IReadOnlyDictionary<UserProfileId, MessagingUserProfile> profiles)
        => profiles.TryGetValue(userProfileId, out var profile)
            ? profile.DisplayName
            : "Utilisateur";

    private static string? ResolveListingTitle(
        Conversation conversation,
        IReadOnlyDictionary<ListingId, ListingMessagingInfo> listings)
        => conversation.ListingId.HasValue
            && listings.TryGetValue(conversation.ListingId.Value, out var listing)
                ? listing.Title
                : null;

    private static string BuildConversationUrl(string? applicationBaseUrl, ConversationId conversationId)
    {
        var path = $"/messagerie/{conversationId.Value:D}";
        return string.IsNullOrWhiteSpace(applicationBaseUrl)
            ? path
            : $"{applicationBaseUrl.TrimEnd('/')}{path}";
    }
}
