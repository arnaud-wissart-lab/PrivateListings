using Microsoft.Extensions.Options;
using PrivateListings.Application.Messaging;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Messaging;
using PrivateListings.Domain.Moderation;

namespace PrivateListings.Application.Tests;

public sealed class MessagingServiceTests
{
    private static readonly DateTimeOffset ReferenceDate = new(2026, 7, 7, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StartListingConversationAsyncCreeConversationDepuisReponseAnnonce()
    {
        var context = CreateContext();
        var repository = new FakeMessagingRepository(context);
        var notificationSender = new CapturingNotificationSender();
        var service = CreateService(repository, notificationSender);
        var body = "Bonjour, je suis intéressé par cette annonce privée.";

        var result = await service.StartListingConversationAsync(new StartListingConversationCommand(
            context.ResponderId,
            context.ListingId,
            body,
            CreateRequestContext()));

        Assert.True(result.Succeeded);
        var conversation = Assert.Single(repository.Conversations);
        Assert.Equal(context.ListingId, conversation.ListingId);
        Assert.Contains(context.OwnerId, conversation.ParticipantIds);
        Assert.Contains(context.ResponderId, conversation.ParticipantIds);
        Assert.Contains(conversation.Messages, message => message.Body == body);
    }

    [Fact]
    public async Task SendMessageAsyncAjouteMessageDansConversation()
    {
        var context = CreateContext();
        var conversation = CreateConversation(context);
        var repository = new FakeMessagingRepository(context, conversation);
        var service = CreateService(repository);

        var result = await service.SendMessageAsync(new SendConversationMessageCommand(
            context.OwnerId,
            conversation.Id,
            "Bonjour, oui, l'annonce est toujours disponible.",
            CreateRequestContext()));

        Assert.True(result.Succeeded);
        Assert.Equal(2, conversation.Messages.Count);
    }

    [Fact]
    public async Task GetConversationAsyncAutoriseUnParticipant()
    {
        var context = CreateContext();
        var conversation = CreateConversation(context);
        var service = CreateService(new FakeMessagingRepository(context, conversation));

        var thread = await service.GetConversationAsync(conversation.Id, context.ResponderId);

        Assert.NotNull(thread);
        Assert.Equal(context.OwnerId, thread.OtherParticipantId);
        Assert.Single(thread.Messages);
    }

    [Fact]
    public async Task GetConversationAsyncRefuseUnTiers()
    {
        var context = CreateContext();
        var conversation = CreateConversation(context);
        var service = CreateService(new FakeMessagingRepository(context, conversation));

        var thread = await service.GetConversationAsync(conversation.Id, context.ThirdUserId);

        Assert.Null(thread);
    }

    [Fact]
    public async Task StartListingConversationAsyncNotifieSansContenuSensible()
    {
        var context = CreateContext();
        var repository = new FakeMessagingRepository(context);
        var notificationSender = new CapturingNotificationSender();
        var service = CreateService(repository, notificationSender);
        var sensitiveBody = "Secret familial à ne jamais envoyer par email.";

        var result = await service.StartListingConversationAsync(new StartListingConversationCommand(
            context.ResponderId,
            context.ListingId,
            sensitiveBody,
            CreateRequestContext()));

        Assert.True(result.Succeeded);
        var notification = Assert.Single(notificationSender.Notifications);
        Assert.Equal("owner@example.test", notification.RecipientEmailAddress);
        Assert.Contains($"/messagerie/{result.ConversationId!.Value.Value:D}", notification.ConversationUrl, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveBody, string.Join('|', notification.RecipientEmailAddress, notification.ConversationUrl));
    }

    [Fact]
    public async Task ReportMessageAsyncCreeUnSignalement()
    {
        var context = CreateContext();
        var conversation = CreateConversation(context);
        var message = conversation.Messages.Single();
        var repository = new FakeMessagingRepository(context, conversation);
        var service = CreateService(repository);

        var result = await service.ReportMessageAsync(new ReportMessageCommand(
            context.OwnerId,
            conversation.Id,
            message.Id,
            "Message à vérifier",
            "Signalement fictif sans recopier le message."));

        Assert.True(result.Succeeded);
        var report = Assert.Single(repository.Reports);
        Assert.Equal(ReportTargetType.Message, report.Target.Type);
        Assert.Equal(message.Id.Value, report.Target.TargetId);
    }

    [Fact]
    public async Task BlockUserAsyncEmpecheLEnvoiEntreParticipants()
    {
        var context = CreateContext();
        var conversation = CreateConversation(context);
        var repository = new FakeMessagingRepository(context, conversation);
        var service = CreateService(repository);

        var blockResult = await service.BlockUserAsync(new BlockUserCommand(
            context.ResponderId,
            conversation.Id,
            context.OwnerId));

        var sendResult = await service.SendMessageAsync(new SendConversationMessageCommand(
            context.OwnerId,
            conversation.Id,
            "Message qui doit être refusé.",
            CreateRequestContext()));

        Assert.True(blockResult.Succeeded);
        Assert.Equal(MessagingOperationStatus.Blocked, sendResult.Status);
        Assert.Single(conversation.Messages);
    }

    [Fact]
    public async Task SendMessageAsyncRetourneRateLimitedQuandLaLimiteEstAtteinte()
    {
        var context = CreateContext();
        var conversation = CreateConversation(context);
        var service = CreateService(
            new FakeMessagingRepository(context, conversation),
            rateLimiter: new DenyingMessagingRateLimiter());

        var result = await service.SendMessageAsync(new SendConversationMessageCommand(
            context.OwnerId,
            conversation.Id,
            "Message refusé par la limite.",
            CreateRequestContext()));

        Assert.Equal(MessagingOperationStatus.RateLimited, result.Status);
        Assert.Single(conversation.Messages);
    }

    [Fact]
    public async Task MarkReadAsyncEtMarkUnreadAsyncPilotentLEtatNonLu()
    {
        var context = CreateContext();
        var conversation = CreateConversation(context);
        conversation.AddMessage(context.OwnerId, "Réponse à lire.", ReferenceDate.AddMinutes(2));
        var repository = new FakeMessagingRepository(context, conversation);
        var service = CreateService(repository);

        var before = await service.GetConversationAsync(conversation.Id, context.ResponderId);
        var readResult = await service.MarkReadAsync(new ConversationParticipantCommand(context.ResponderId, conversation.Id));
        var afterRead = await service.GetConversationAsync(conversation.Id, context.ResponderId);
        var unreadResult = await service.MarkUnreadAsync(new ConversationParticipantCommand(context.ResponderId, conversation.Id));
        var afterUnread = await service.GetConversationAsync(conversation.Id, context.ResponderId);

        Assert.True(before!.IsUnread);
        Assert.True(readResult.Succeeded);
        Assert.False(afterRead!.IsUnread);
        Assert.True(unreadResult.Succeeded);
        Assert.True(afterUnread!.IsUnread);
    }

    private static MessagingService CreateService(
        FakeMessagingRepository repository,
        CapturingNotificationSender? notificationSender = null,
        IMessagingRateLimiter? rateLimiter = null)
        => new(
            repository,
            rateLimiter ?? new AllowingMessagingRateLimiter(),
            notificationSender ?? new CapturingNotificationSender(),
            Options.Create(new MessagingSpamOptions()),
            new FixedTimeProvider(ReferenceDate.AddHours(1)));

    private static Conversation CreateConversation(TestMessagingContext context)
    {
        var conversation = Conversation.StartForListing(
            context.ListingId,
            context.ResponderId,
            context.OwnerId,
            ReferenceDate);
        conversation.AddMessage(context.ResponderId, "Bonjour, message initial fictif.", ReferenceDate.AddMinutes(1));

        return conversation;
    }

    private static TestMessagingContext CreateContext()
        => new(
            ListingId.New(),
            UserProfileId.New(),
            UserProfileId.New(),
            UserProfileId.New());

    private static MessagingRequestContext CreateRequestContext()
        => new("127.0.0.1", "PrivateListings.Tests", "https://privatelistings.example.test");

    private sealed record TestMessagingContext(
        ListingId ListingId,
        UserProfileId OwnerId,
        UserProfileId ResponderId,
        UserProfileId ThirdUserId);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class AllowingMessagingRateLimiter : IMessagingRateLimiter
    {
        public Task<MessagingRateLimitLease> TryAcquireAsync(
            MessagingRateLimitRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new MessagingRateLimitLease(true));
    }

    private sealed class DenyingMessagingRateLimiter : IMessagingRateLimiter
    {
        public Task<MessagingRateLimitLease> TryAcquireAsync(
            MessagingRateLimitRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new MessagingRateLimitLease(false, TimeSpan.FromMinutes(5)));
    }

    private sealed class CapturingNotificationSender : IMessageNotificationSender
    {
        public List<MessageNotification> Notifications { get; } = [];

        public Task SendNewMessageNotificationAsync(
            MessageNotification notification,
            CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMessagingRepository : IMessagingRepository
    {
        private readonly TestMessagingContext context;
        private readonly List<UserBlock> userBlocks = [];

        public FakeMessagingRepository(TestMessagingContext context, params Conversation[] conversations)
        {
            this.context = context;
            Conversations.AddRange(conversations);
        }

        public List<Conversation> Conversations { get; } = [];

        public List<Report> Reports { get; } = [];

        public Task<ListingConversationContext?> GetListingConversationContextAsync(
            ListingId listingId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
            => Task.FromResult<ListingConversationContext?>(
                listingId == context.ListingId
                    ? new ListingConversationContext(context.ListingId, context.OwnerId, "Annonce fictive")
                    : null);

        public Task<Conversation?> FindListingConversationForUpdateAsync(
            ListingId listingId,
            UserProfileId actorId,
            UserProfileId listingOwnerId,
            CancellationToken cancellationToken)
            => Task.FromResult(Conversations.FirstOrDefault(conversation =>
                conversation.ListingId == listingId
                && conversation.ParticipantIds.Contains(actorId)
                && conversation.ParticipantIds.Contains(listingOwnerId)));

        public Task<Conversation?> GetConversationForUpdateAsync(
            ConversationId conversationId,
            CancellationToken cancellationToken)
            => Task.FromResult(Conversations.SingleOrDefault(conversation => conversation.Id == conversationId));

        public Task<IReadOnlyList<Conversation>> ListConversationsForParticipantAsync(
            UserProfileId actorId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Conversation>>(
                Conversations.Where(conversation => conversation.ParticipantIds.Contains(actorId)).ToList());

        public Task<IReadOnlyDictionary<UserProfileId, MessagingUserProfile>> GetUserProfilesByIdAsync(
            IReadOnlyCollection<UserProfileId> userProfileIds,
            CancellationToken cancellationToken)
        {
            var profiles = new[]
            {
                new MessagingUserProfile(context.OwnerId, "Propriétaire", "owner@example.test"),
                new MessagingUserProfile(context.ResponderId, "Répondant", "responder@example.test"),
                new MessagingUserProfile(context.ThirdUserId, "Tiers", "third@example.test")
            };

            return Task.FromResult<IReadOnlyDictionary<UserProfileId, MessagingUserProfile>>(
                profiles
                    .Where(profile => userProfileIds.Contains(profile.Id))
                    .ToDictionary(profile => profile.Id));
        }

        public Task<IReadOnlyDictionary<ListingId, ListingMessagingInfo>> GetListingInfosByIdAsync(
            IReadOnlyCollection<ListingId> listingIds,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<ListingId, ListingMessagingInfo>>(
                listingIds.Contains(context.ListingId)
                    ? new Dictionary<ListingId, ListingMessagingInfo>
                    {
                        [context.ListingId] = new(context.ListingId, "Annonce fictive")
                    }
                    : new Dictionary<ListingId, ListingMessagingInfo>());

        public Task<UserBlock?> GetActiveBlockBetweenAsync(
            UserProfileId firstUserId,
            UserProfileId secondUserId,
            CancellationToken cancellationToken)
            => Task.FromResult(userBlocks.SingleOrDefault(userBlock =>
                userBlock.IsActive
                && (userBlock.BlockerId == firstUserId && userBlock.BlockedId == secondUserId
                    || userBlock.BlockerId == secondUserId && userBlock.BlockedId == firstUserId)));

        public Task<UserBlock?> GetUserBlockAsync(
            UserProfileId blockerId,
            UserProfileId blockedId,
            CancellationToken cancellationToken)
            => Task.FromResult(userBlocks.SingleOrDefault(userBlock =>
                userBlock.BlockerId == blockerId && userBlock.BlockedId == blockedId));

        public Task<bool> HasRecentDuplicateMessageAsync(
            ConversationId conversationId,
            UserProfileId senderId,
            string body,
            DateTimeOffset since,
            CancellationToken cancellationToken)
            => Task.FromResult(Conversations
                .Where(conversation => conversation.Id == conversationId)
                .SelectMany(conversation => conversation.Messages)
                .Any(message => message.SenderId == senderId
                    && !message.IsDeleted
                    && message.SentAt >= since
                    && message.Body == body));

        public Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken)
        {
            Conversations.Add(conversation);
            return Task.CompletedTask;
        }

        public Task AddReportAsync(Report report, CancellationToken cancellationToken)
        {
            Reports.Add(report);
            return Task.CompletedTask;
        }

        public Task AddUserBlockAsync(UserBlock userBlock, CancellationToken cancellationToken)
        {
            userBlocks.Add(userBlock);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
