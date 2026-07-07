using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrivateListings.Application.Messaging;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;
using PrivateListings.Domain.Users;
using PrivateListings.Infrastructure.Persistence;

namespace PrivateListings.IntegrationTests;

[Collection(PostgreSqlTestCollectionDefinition.Name)]
public sealed class MessagingModuleIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ReponseAnnonceCreeConversationAccessibleAuxParticipants()
    {
        await fixture.ResetDatabaseAsync();
        await using var serviceProvider = fixture.CreateServiceProvider(
            configureServices: services =>
                services.AddSingleton<IMessageNotificationSender, CapturingMessageNotificationSender>());
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var messagingService = scope.ServiceProvider.GetRequiredService<IMessagingService>();
        var notificationSender = scope.ServiceProvider.GetRequiredService<IMessageNotificationSender>() as CapturingMessageNotificationSender
            ?? throw new InvalidOperationException("Le notificateur de test n'est pas enregistré.");
        var now = DateTimeOffset.UtcNow;
        var owner = CreateUser("owner-messaging@example.test", "Propriétaire Messagerie", now.AddDays(-2));
        var responder = CreateUser("responder-messaging@example.test", "Répondant Messagerie", now.AddDays(-2));
        var thirdUser = CreateUser("third-messaging@example.test", "Tiers Messagerie", now.AddDays(-2));
        var category = ListingCategory.Create("Messagerie", now.AddDays(-2));
        var listing = Listing.Create(
            owner.Id,
            category.Id,
            "Objet de test messagerie",
            "Annonce publiée pour vérifier la création de conversation.",
            ListingType.Give,
            now.AddDays(-1),
            now.AddDays(10),
            "Quartier test");
        listing.Publish(now.AddHours(-2));
        dbContext.UserProfiles.AddRange(owner, responder, thirdUser);
        dbContext.ListingCategories.Add(category);
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        var result = await messagingService.StartListingConversationAsync(new StartListingConversationCommand(
            responder.Id,
            listing.Id,
            "Bonjour, message privé de test.",
            new MessagingRequestContext("127.0.0.1", "PrivateListings.IntegrationTests", "https://privatelistings.example.test")));

        Assert.True(result.Succeeded);

        dbContext.ChangeTracker.Clear();
        var conversation = await dbContext.Conversations
            .Include(value => value.Messages)
            .Include(value => value.ReadStates)
            .SingleAsync(value => value.Id == result.ConversationId!.Value);
        var ownerConversations = await messagingService.ListConversationsAsync(owner.Id);
        var responderConversations = await messagingService.ListConversationsAsync(responder.Id);
        var thirdThread = await messagingService.GetConversationAsync(conversation.Id, thirdUser.Id);

        Assert.Equal(2, conversation.ReadStates.Count);
        Assert.Single(conversation.Messages);
        Assert.Contains(ownerConversations, value => value.Id == conversation.Id);
        Assert.Contains(responderConversations, value => value.Id == conversation.Id);
        Assert.Null(thirdThread);
        var notification = Assert.Single(notificationSender.Notifications);
        Assert.Equal("owner-messaging@example.test", notification.RecipientEmailAddress);
        Assert.DoesNotContain("Bonjour, message privé de test.", notification.ConversationUrl, StringComparison.Ordinal);
    }

    private static UserProfile CreateUser(string email, string displayName, DateTimeOffset createdAt)
        => UserProfile.Create(
            EmailAddress.Create(email),
            displayName,
            createdAt,
            "Profil fictif créé par un test d'intégration.");

    private sealed class CapturingMessageNotificationSender : IMessageNotificationSender
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
}
