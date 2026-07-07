using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;
using PrivateListings.Domain.Messaging;
using PrivateListings.Domain.Moderation;
using PrivateListings.Domain.Users;
using PrivateListings.Infrastructure.Persistence;
using PrivateListings.Infrastructure.Persistence.Seeding;

namespace PrivateListings.IntegrationTests;

[Collection(PostgreSqlTestCollectionDefinition.Name)]
public sealed class PrivateListingsPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset ReferenceDate = new(2026, 7, 7, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DbContextDemarreCorrectement()
    {
        await fixture.ResetDatabaseAsync();

        await using var dbContext = fixture.CreateDbContext();

        Assert.True(await dbContext.Database.CanConnectAsync());
    }

    [Fact]
    public async Task MigrationsSontApplicables()
    {
        await fixture.ResetDatabaseAsync();

        await using var dbContext = fixture.CreateDbContext();
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreationAnnonceEstPersistee()
    {
        await fixture.ResetDatabaseAsync();

        await using var dbContext = fixture.CreateDbContext();
        var owner = CreateUser("proprietaire-annonce@example.test", "Propriétaire Démo");
        var category = ListingCategory.Create("Vente", ReferenceDate, "Catégorie fictive de test.");
        var listing = Listing.Create(
            owner.Id,
            category.Id,
            "Étagère fictive à vendre",
            "Annonce fictive sans adresse réelle ni numéro de téléphone.",
            ListingType.Sell,
            ReferenceDate,
            ReferenceDate.AddDays(30));
        listing.Publish(ReferenceDate.AddHours(1));

        dbContext.UserProfiles.Add(owner);
        dbContext.ListingCategories.Add(category);
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();
        var persistedListing = await dbContext.Listings.SingleAsync(value => value.Id == listing.Id);

        Assert.Equal(ListingStatus.Published, persistedListing.Status);
        Assert.Equal(owner.Id, persistedListing.OwnerId);
        Assert.Equal(category.Id, persistedListing.CategoryId);
    }

    [Fact]
    public async Task ConversationEstPersistee()
    {
        await fixture.ResetDatabaseAsync();

        await using var dbContext = fixture.CreateDbContext();
        var initiator = CreateUser("initiateur-conversation@example.test", "Initiateur Démo");
        var recipient = CreateUser("destinataire-conversation@example.test", "Destinataire Démo");
        var conversation = Conversation.StartDirect(initiator.Id, recipient.Id, ReferenceDate);
        conversation.AddMessage(initiator.Id, "Bonjour, ceci est un message fictif.", ReferenceDate.AddMinutes(2));
        conversation.AddMessage(recipient.Id, "Bonjour, réponse fictive bien reçue.", ReferenceDate.AddMinutes(5));

        dbContext.UserProfiles.AddRange(initiator, recipient);
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();
        var persistedConversation = await dbContext.Conversations
            .Include(value => value.Messages)
            .SingleAsync(value => value.Id == conversation.Id);

        Assert.Equal(2, persistedConversation.ParticipantIds.Count);
        Assert.Equal(2, persistedConversation.Messages.Count);
    }

    [Fact]
    public async Task SignalementEstPersiste()
    {
        await fixture.ResetDatabaseAsync();

        await using var dbContext = fixture.CreateDbContext();
        var reporter = CreateUser("rapporteur@example.test", "Rapporteur Démo");
        var owner = CreateUser("proprietaire-signale@example.test", "Propriétaire Signalé Démo");
        var category = ListingCategory.Create("Services", ReferenceDate, "Catégorie fictive de test.");
        var listing = Listing.Create(
            owner.Id,
            category.Id,
            "Aide fictive à tester",
            "Annonce de service fictive destinée au test de signalement.",
            ListingType.Service,
            ReferenceDate);
        var report = Report.Create(
            reporter.Id,
            ReportTarget.ForListing(listing.Id),
            "Information à vérifier",
            ReferenceDate.AddHours(2),
            "Signalement fictif sans contenu sensible.");

        dbContext.UserProfiles.AddRange(reporter, owner);
        dbContext.ListingCategories.Add(category);
        dbContext.Listings.Add(listing);
        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();
        var persistedReport = await dbContext.Reports.SingleAsync(value => value.Id == report.Id);

        Assert.Equal(ReportStatus.Open, persistedReport.Status);
        Assert.Equal(ReportTargetType.Listing, persistedReport.Target.Type);
        Assert.Equal(listing.Id.Value, persistedReport.Target.TargetId);
    }

    [Fact]
    public async Task SeedDemoEstExecutableEnEnvironnementDemo()
    {
        await fixture.ResetDatabaseAsync();

        await using var dbContext = fixture.CreateDbContext();
        var referenceSeeder = new PrivateListingsReferenceDataSeeder(dbContext);
        var demoSeeder = new PrivateListingsDemoDataSeeder(dbContext, referenceSeeder);

        await demoSeeder.SeedAsync(new TestHostEnvironment("Demo"), CreateDemoSeedOptions());

        Assert.Equal(7, await dbContext.ListingCategories.CountAsync());
        Assert.Equal(20, await dbContext.UserProfiles.CountAsync());
        Assert.Equal(80, await dbContext.Listings.CountAsync());
        Assert.Equal(3, await dbContext.DemoUserAccounts.CountAsync());
        Assert.True(await dbContext.Conversations.AnyAsync());
        Assert.True(await dbContext.Messages.AnyAsync());
        Assert.True(await dbContext.Reports.AnyAsync());
    }

    [Fact]
    public async Task SeedDemoNestPasExecuteParDefautEnProduction()
    {
        await fixture.ResetDatabaseAsync();

        await using var dbContext = fixture.CreateDbContext();
        var referenceSeeder = new PrivateListingsReferenceDataSeeder(dbContext);
        var demoSeeder = new PrivateListingsDemoDataSeeder(dbContext, referenceSeeder);
        var initializer = new PrivateListingsDatabaseInitializer(
            dbContext,
            referenceSeeder,
            demoSeeder,
            new TestHostEnvironment(Environments.Production),
            Options.Create(new PrivateListingsPersistenceOptions()),
            Options.Create(new PrivateListingsReferenceDataOptions()),
            Options.Create(new PrivateListingsDemoSeedOptions()));

        await initializer.InitializeAsync();

        Assert.False(await dbContext.UserProfiles.AnyAsync());
        Assert.False(await dbContext.DemoUserAccounts.AnyAsync());
        Assert.False(await dbContext.Listings.AnyAsync());
    }

    private static UserProfile CreateUser(string email, string displayName)
        => UserProfile.Create(
            EmailAddress.Create(email),
            displayName,
            ReferenceDate,
            "Profil fictif créé par un test d'intégration.");

    private static PrivateListingsDemoSeedOptions CreateDemoSeedOptions()
        => new()
        {
            Enabled = true,
            AllowInProduction = false,
            Accounts = new PrivateListingsDemoSeedAccountsOptions
            {
                Admin = new PrivateListingsDemoSeedAccountOptions
                {
                    Email = "admin-demo@example.test",
                    DisplayName = "Administrateur Démo",
                    Password = "DemoAdmin!2026"
                },
                Moderator = new PrivateListingsDemoSeedAccountOptions
                {
                    Email = "moderateur-demo@example.test",
                    DisplayName = "Modérateur Démo",
                    Password = "DemoModerateur!2026"
                },
                Member = new PrivateListingsDemoSeedAccountOptions
                {
                    Email = "membre-demo@example.test",
                    DisplayName = "Membre Démo",
                    Password = "DemoMembre!2026"
                }
            }
        };
}
