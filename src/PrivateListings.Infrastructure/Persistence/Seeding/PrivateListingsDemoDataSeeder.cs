using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PrivateListings.Domain.Auditing;
using PrivateListings.Domain.Listings;
using PrivateListings.Domain.Messaging;
using PrivateListings.Domain.Moderation;
using PrivateListings.Domain.Privacy;
using PrivateListings.Domain.Recommendations;
using PrivateListings.Domain.Users;
using PrivateListings.Infrastructure.Identity;

namespace PrivateListings.Infrastructure.Persistence.Seeding;

public sealed class PrivateListingsDemoDataSeeder(
    PrivateListingsDbContext dbContext,
    PrivateListingsReferenceDataSeeder referenceDataSeeder,
    PrivateListingsIdentitySeeder identitySeeder,
    UserManager<PrivateListingsUser> userManager)
{
    private static readonly DateTimeOffset SeedDate = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    public async Task SeedAsync(
        IHostEnvironment environment,
        PrivateListingsDemoSeedOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!PrivateListingsDemoSeedPolicy.ShouldRun(environment, options))
        {
            return;
        }

        ValidateAccountOptions(options);

        await referenceDataSeeder.SeedAsync(cancellationToken).ConfigureAwait(false);
        await identitySeeder.SeedAsync(cancellationToken).ConfigureAwait(false);

        if (await dbContext.DemoUserAccounts.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            await SeedIdentityAccountsAsync(options, cancellationToken).ConfigureAwait(false);
            return;
        }

        var categories = await dbContext.ListingCategories
            .ToDictionaryAsync(category => category.Name, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);

        var users = CreateUsers(options);
        await dbContext.UserProfiles.AddRangeAsync(users, cancellationToken).ConfigureAwait(false);

        var accounts = CreateAccounts(options, users);
        await dbContext.DemoUserAccounts.AddRangeAsync(accounts, cancellationToken).ConfigureAwait(false);

        var listings = CreateListings(users, categories);
        await dbContext.Listings.AddRangeAsync(listings, cancellationToken).ConfigureAwait(false);

        var conversations = CreateConversations(users, listings);
        await dbContext.Conversations.AddRangeAsync(conversations, cancellationToken).ConfigureAwait(false);

        var reports = CreateReports(users, listings, conversations);
        await dbContext.Reports.AddRangeAsync(reports, cancellationToken).ConfigureAwait(false);

        var recommendations = CreateRecommendations(users, listings);
        await dbContext.Recommendations.AddRangeAsync(recommendations, cancellationToken).ConfigureAwait(false);

        var auditLogs = CreateAuditLogs(users);
        await dbContext.AuditLogs.AddRangeAsync(auditLogs, cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await SeedIdentityAccountsAsync(options, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateAccountOptions(PrivateListingsDemoSeedOptions options)
    {
        ValidateAccount("admin", options.Accounts.Admin);
        ValidateAccount("modérateur", options.Accounts.Moderator);
        ValidateAccount("membre", options.Accounts.Member);
    }

    private static void ValidateAccount(string label, PrivateListingsDemoSeedAccountOptions account)
    {
        if (string.IsNullOrWhiteSpace(account.Email)
            || string.IsNullOrWhiteSpace(account.DisplayName)
            || string.IsNullOrWhiteSpace(account.Password))
        {
            throw new InvalidOperationException(
                $"La configuration du compte démo {label} doit définir un email, un nom d'affichage et un mot de passe fictifs.");
        }
    }

    private static List<UserProfile> CreateUsers(PrivateListingsDemoSeedOptions options)
    {
        var users = new List<UserProfile>
        {
            CreateUser(options.Accounts.Admin.Email, options.Accounts.Admin.DisplayName, "Profil fictif d'administration pour la démonstration."),
            CreateUser(options.Accounts.Moderator.Email, options.Accounts.Moderator.DisplayName, "Profil fictif de modération pour la démonstration."),
            CreateUser(options.Accounts.Member.Email, options.Accounts.Member.DisplayName, "Profil fictif de membre pour la démonstration.")
        };

        for (var index = 4; index <= 20; index++)
        {
            users.Add(CreateUser(
                $"membre-demo-{index:00}@example.test",
                $"Membre Démo {index:00}",
                $"Profil fictif numéro {index:00}, utilisé uniquement pour les scénarios de démonstration."));
        }

        return users;
    }

    private static UserProfile CreateUser(string email, string displayName, string bio)
    {
        var user = UserProfile.Create(EmailAddress.Create(email), displayName, SeedDate, bio);
        user.RecordConsent(ConsentType.TermsOfService, true, "demo-2026.07", "seed-demo", SeedDate.AddMinutes(1));
        user.RecordConsent(ConsentType.PrivacyPolicy, true, "demo-2026.07", "seed-demo", SeedDate.AddMinutes(2));

        return user;
    }

    private static IReadOnlyList<DemoUserAccount> CreateAccounts(
        PrivateListingsDemoSeedOptions options,
        List<UserProfile> users)
        =>
        [
            DemoUserAccount.Create(users[0].Id, options.Accounts.Admin.Email, DemoUserRole.Admin, options.Accounts.Admin.Password, SeedDate),
            DemoUserAccount.Create(users[1].Id, options.Accounts.Moderator.Email, DemoUserRole.Moderator, options.Accounts.Moderator.Password, SeedDate),
            DemoUserAccount.Create(users[2].Id, options.Accounts.Member.Email, DemoUserRole.Member, options.Accounts.Member.Password, SeedDate)
        ];

    private async Task SeedIdentityAccountsAsync(
        PrivateListingsDemoSeedOptions options,
        CancellationToken cancellationToken)
    {
        var accounts = await dbContext.DemoUserAccounts
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var account in accounts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await userManager.FindByEmailAsync(account.Email).ConfigureAwait(false) is not null)
            {
                continue;
            }

            var profile = await dbContext.UserProfiles
                .SingleAsync(userProfile => userProfile.Id == account.UserProfileId, cancellationToken)
                .ConfigureAwait(false);

            var identityUser = new PrivateListingsUser
            {
                Id = Guid.NewGuid(),
                UserName = account.Email,
                Email = account.Email,
                EmailConfirmed = true,
                UserProfileId = account.UserProfileId,
                DisplayName = profile.DisplayName,
                CreatedAt = account.CreatedAt,
                MfaRequiredForAdmin = account.Role == DemoUserRole.Admin
            };

            var createResult = await userManager
                .CreateAsync(identityUser, GetPassword(options, account.Role))
                .ConfigureAwait(false);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Le compte Identity démo '{account.Email}' n'a pas pu être créé : {FormatIdentityErrors(createResult)}");
            }

            var roleResult = await userManager
                .AddToRoleAsync(identityUser, GetRoleName(account.Role))
                .ConfigureAwait(false);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Le rôle du compte Identity démo '{account.Email}' n'a pas pu être créé : {FormatIdentityErrors(roleResult)}");
            }
        }
    }

    private static string GetPassword(PrivateListingsDemoSeedOptions options, DemoUserRole role)
        => role switch
        {
            DemoUserRole.Admin => options.Accounts.Admin.Password,
            DemoUserRole.Moderator => options.Accounts.Moderator.Password,
            DemoUserRole.Member => options.Accounts.Member.Password,
            _ => throw new InvalidOperationException("Le rôle démo est inconnu.")
        };

    private static string GetRoleName(DemoUserRole role)
        => role switch
        {
            DemoUserRole.Admin => PrivateListingsRoleNames.Admin,
            DemoUserRole.Moderator => PrivateListingsRoleNames.Moderator,
            DemoUserRole.Member => PrivateListingsRoleNames.Member,
            _ => throw new InvalidOperationException("Le rôle démo est inconnu.")
        };

    private static string FormatIdentityErrors(IdentityResult result)
        => string.Join(", ", result.Errors.Select(error => error.Description));

    private static List<Listing> CreateListings(
        List<UserProfile> users,
        Dictionary<string, ListingCategory> categories)
    {
        var categoryCycle = new[]
        {
            ("Vente", ListingType.Sell),
            ("Don", ListingType.Give),
            ("Recherche", ListingType.Search),
            ("Services", ListingType.Service),
            ("Événements", ListingType.Event),
            ("Emploi / missions", ListingType.Job),
            ("Projets / entraide", ListingType.Project)
        };

        var listings = new List<Listing>(capacity: 80);

        for (var index = 0; index < 80; index++)
        {
            var (categoryName, listingType) = categoryCycle[index % categoryCycle.Length];
            var owner = users[(index + 3) % users.Count];
            var createdAt = SeedDate.AddDays(-(index % 30)).AddMinutes(index);
            var expiresAt = createdAt.AddYears(2).AddDays(index % 20);
            var listing = Listing.Create(
                owner.Id,
                categories[categoryName].Id,
                BuildListingTitle(listingType, index),
                BuildListingDescription(listingType, index),
                listingType,
                createdAt,
                expiresAt);

            if (index % 5 != 0)
            {
                listing.Publish(createdAt.AddHours(1));
            }

            if (index % 4 == 0)
            {
                listing.AddImage(
                    ListingImage.Create(
                        listing.Id,
                        $"demo/listings/{listing.Id}/image-01.webp",
                        $"image-demo-{index + 1:00}.webp",
                        "image/webp",
                        128_000 + index,
                        0,
                        createdAt.AddMinutes(20),
                        "Image fictive de démonstration."),
                    createdAt.AddMinutes(25));
            }

            listings.Add(listing);
        }

        return listings;
    }

    private static string BuildListingTitle(ListingType type, int index) => type switch
    {
        ListingType.Sell => $"Lot de rangement fictif {index + 1:00}",
        ListingType.Give => $"Objet à donner pour atelier démo {index + 1:00}",
        ListingType.Search => $"Recherche coup de main fictif {index + 1:00}",
        ListingType.Service => $"Aide pratique de voisinage {index + 1:00}",
        ListingType.Event => $"Atelier convivial démo {index + 1:00}",
        ListingType.Job => $"Mission courte fictive {index + 1:00}",
        ListingType.Project => $"Projet d'entraide démo {index + 1:00}",
        _ => $"Annonce fictive {index + 1:00}"
    };

    private static string BuildListingDescription(ListingType type, int index)
    {
        var area = $"secteur fictif {index % 6 + 1}";
        return type switch
        {
            ListingType.Sell => $"Annonce fictive pour un lot en bon état, disponible dans le {area}. Aucun contact réel n'est fourni.",
            ListingType.Give => $"Don fictif réservé aux scénarios de démonstration. Remise organisée dans un lieu générique du {area}.",
            ListingType.Search => $"Demande fictive d'entraide locale pour tester les échanges entre membres du {area}.",
            ListingType.Service => $"Service fictif de proximité, sans adresse réelle ni numéro de téléphone, prévu pour le {area}.",
            ListingType.Event => $"Événement fictif de démonstration, sans lieu réel, conçu pour vérifier l'affichage des annonces.",
            ListingType.Job => $"Mission fictive et ponctuelle, sans employeur réel, utilisée uniquement pour les tests de parcours.",
            ListingType.Project => $"Projet fictif d'entraide locale pour simuler une coordination entre membres du {area}.",
            _ => $"Contenu fictif de démonstration pour l'annonce {index + 1:00}."
        };
    }

    private static List<Conversation> CreateConversations(
        List<UserProfile> users,
        List<Listing> listings)
    {
        var conversations = new List<Conversation>();

        for (var index = 0; index < 8; index++)
        {
            var listing = listings[index * 3];
            var initiator = users[(index + 7) % users.Count];
            if (initiator.Id == listing.OwnerId)
            {
                initiator = users[(index + 8) % users.Count];
            }

            var createdAt = SeedDate.AddHours(index + 2);
            var conversation = Conversation.StartForListing(listing.Id, initiator.Id, listing.OwnerId, createdAt);
            conversation.AddMessage(initiator.Id, "Bonjour, ce message fictif sert à tester la messagerie de démonstration.", createdAt.AddMinutes(4));
            conversation.AddMessage(listing.OwnerId, "Bonjour, merci pour votre message fictif. L'annonce est toujours visible en démo.", createdAt.AddMinutes(17));

            if (index % 2 == 0)
            {
                conversation.AddMessage(initiator.Id, "Parfait, je confirme que ce scénario ne contient aucune donnée réelle.", createdAt.AddMinutes(31));
            }

            conversations.Add(conversation);
        }

        return conversations;
    }

    private static List<Report> CreateReports(
        List<UserProfile> users,
        List<Listing> listings,
        List<Conversation> conversations)
    {
        var reports = new List<Report>
        {
            Report.Create(
                users[6].Id,
                ReportTarget.ForListing(listings[4].Id),
                "Information à vérifier",
                SeedDate.AddHours(12),
                "Signalement fictif : la description de démonstration mérite une vérification."),
            Report.Create(
                users[8].Id,
                ReportTarget.ForListing(listings[11].Id),
                "Catégorie possiblement incorrecte",
                SeedDate.AddHours(13),
                "Signalement fictif : la catégorie pourrait être ajustée dans le scénario."),
            Report.Create(
                users[9].Id,
                ReportTarget.ForUserProfile(users[10].Id),
                "Profil de démonstration à relire",
                SeedDate.AddHours(14),
                "Signalement fictif sans personne réelle ni donnée sensible.")
        };

        var firstMessage = conversations[0].Messages.First();
        reports.Add(Report.Create(
            users[12].Id,
            ReportTarget.ForMessage(firstMessage.Id),
            "Message fictif à relire",
            SeedDate.AddHours(15),
            "Signalement fictif pour tester la modération des messages."));

        reports[0].MarkUnderReview(users[1].Id, SeedDate.AddHours(16));
        reports[0].Resolve(users[1].Id, "Signalement fictif traité pendant le seed démo.", SeedDate.AddHours(17));

        return reports;
    }

    private static List<Recommendation> CreateRecommendations(
        List<UserProfile> users,
        List<Listing> listings)
    {
        var recommendations = new List<Recommendation>();

        for (var index = 0; index < 12; index++)
        {
            recommendations.Add(Recommendation.Create(
                users[(index + 5) % users.Count].Id,
                listings[(index * 5) % listings.Count].Id,
                0.55m + (index % 4 * 0.1m),
                SeedDate.AddHours(18).AddMinutes(index),
                "Recommandation fictive générée pour la démonstration."));
        }

        return recommendations;
    }

    private static IReadOnlyList<AuditLog> CreateAuditLogs(List<UserProfile> users)
        =>
        [
            AuditLog.Create(
                users[0].Id,
                "Demo.SeedExecuted",
                AuditLogSubject.ForSystem("DemoSeed"),
                SeedDate.AddHours(20),
                "Seed de démonstration fictif exécuté hors production."),
            AuditLog.Create(
                users[1].Id,
                "Report.Resolved",
                AuditLogSubject.ForSystem("ModerationDemo"),
                SeedDate.AddHours(21),
                "Action de modération fictive créée par le seed démo.")
        ];
}
