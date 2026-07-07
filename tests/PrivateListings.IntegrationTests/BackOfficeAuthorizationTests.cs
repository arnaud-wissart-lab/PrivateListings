using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrivateListings.Application.BackOffice;
using PrivateListings.Application.Listings;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;
using PrivateListings.Domain.Moderation;
using PrivateListings.Domain.Users;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Infrastructure.Persistence;
using PrivateListings.Infrastructure.Persistence.Seeding;
using PrivateListings.Web.Admin;
using PrivateListings.Web.Components.Pages;

namespace PrivateListings.IntegrationTests;

[Collection(PostgreSqlTestCollectionDefinition.Name)]
public sealed class BackOfficeAuthorizationTests(PostgreSqlFixture fixture)
{
    private const string ValidPassword = "phrase de passe privee 2026";
    private static readonly DateTimeOffset ReferenceDate = new(2026, 7, 7, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PolicyAdminRefuseUnMembre()
    {
        await using var serviceProvider = CreateAuthorizationServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();

        var result = await authorizationService.AuthorizeAsync(
            CreatePrincipal(PrivateListingsRoleNames.Member),
            resource: null,
            BackOfficePolicies.Admin);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task PolicyModerationAutoriseUnModerateur()
    {
        await using var serviceProvider = CreateAuthorizationServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();

        var result = await authorizationService.AuthorizeAsync(
            CreatePrincipal(PrivateListingsRoleNames.Moderator),
            resource: null,
            BackOfficePolicies.Moderation);

        Assert.True(result.Succeeded);
        AssertPolicy<AdminReports>(BackOfficePolicies.Moderation);
        AssertPolicy<AdminListings>(BackOfficePolicies.Moderation);
    }

    [Fact]
    public void EcransAdminSontProtegesParPolicyAdmin()
    {
        AssertPolicy<AdminDashboard>(BackOfficePolicies.Admin);
        AssertPolicy<AdminUsers>(BackOfficePolicies.Admin);
        AssertPolicy<AdminUserDetails>(BackOfficePolicies.Admin);
        AssertPolicy<AdminAuditLogs>(BackOfficePolicies.Admin);
        AssertPolicy<AdminCategories>(BackOfficePolicies.Admin);
    }

    [Fact]
    public async Task ModerateurEstBloqueSurActionReserveeAdmin()
    {
        await fixture.ResetDatabaseAsync();
        await using var serviceProvider = fixture.CreateServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var backOfficeService = scope.ServiceProvider.GetRequiredService<IBackOfficeService>();

        var result = await backOfficeService.SuspendUserAsync(new SuspendBackOfficeUserCommand(
            new BackOfficeActor(UserProfileId.New(), IsAdmin: false, IsModerator: true),
            Guid.NewGuid(),
            "Test de refus."));

        Assert.Equal(BackOfficeOperationStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task SuspensionUtilisateurCreeAuditLog()
    {
        await fixture.ResetDatabaseAsync();
        await using var serviceProvider = fixture.CreateServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PrivateListingsUser>>();
        var backOfficeService = scope.ServiceProvider.GetRequiredService<IBackOfficeService>();
        var admin = await CreateIdentityUserAsync(
            dbContext,
            userManager,
            "admin-audit@example.test",
            "Admin Audit",
            PrivateListingsRoleNames.Admin);
        var member = await CreateIdentityUserAsync(
            dbContext,
            userManager,
            "membre-audit@example.test",
            "Membre Audit",
            PrivateListingsRoleNames.Member);

        var result = await backOfficeService.SuspendUserAsync(new SuspendBackOfficeUserCommand(
            new BackOfficeActor(admin.UserProfileId, IsAdmin: true, IsModerator: false),
            member.Id,
            "Compte de test suspendu."));

        Assert.True(result.Succeeded);
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog =>
            auditLog.Action == "User.Suspended"
            && auditLog.ActorUserProfileId == admin.UserProfileId));
    }

    [Fact]
    public async Task SuspensionAnnonceParBackOfficeMasqueAnnoncePublique()
    {
        await fixture.ResetDatabaseAsync();
        await using var serviceProvider = fixture.CreateServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var backOfficeService = scope.ServiceProvider.GetRequiredService<IBackOfficeService>();
        var listingService = scope.ServiceProvider.GetRequiredService<IListingService>();
        var actor = CreateUser("moderateur-annonce@example.test", "Modérateur Annonce");
        var owner = CreateUser("proprietaire-annonce-bo@example.test", "Propriétaire Annonce");
        var category = ListingCategory.Create("Modération BO", ReferenceDate);
        var listing = Listing.Create(
            owner.Id,
            category.Id,
            "Annonce à masquer",
            "Annonce publiée qui doit disparaître après action back-office.",
            ListingType.Give,
            ReferenceDate,
            ReferenceDate.AddDays(30),
            "Quartier test");
        listing.Publish(ReferenceDate.AddHours(1));
        dbContext.UserProfiles.AddRange(actor, owner);
        dbContext.ListingCategories.Add(category);
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        var result = await backOfficeService.SuspendListingAsync(new SuspendBackOfficeListingCommand(
            new BackOfficeActor(actor.Id, IsAdmin: false, IsModerator: true),
            listing.Id,
            "Annonce suspendue par test."));
        var publicListings = await listingService.SearchPublishedAsync(new ListingSearchQuery(Text: "Annonce à masquer"));

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(publicListings, value => value.Id == listing.Id);
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Listing.Suspended"));
    }

    [Fact]
    public async Task TraitementSignalementChangeSonStatut()
    {
        await fixture.ResetDatabaseAsync();
        await using var serviceProvider = fixture.CreateServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var backOfficeService = scope.ServiceProvider.GetRequiredService<IBackOfficeService>();
        var moderator = CreateUser("moderateur-report@example.test", "Modérateur Report");
        var reporter = CreateUser("rapporteur-bo@example.test", "Rapporteur BO");
        var owner = CreateUser("proprietaire-report@example.test", "Propriétaire Report");
        var category = ListingCategory.Create("Signalement BO", ReferenceDate);
        var listing = Listing.Create(
            owner.Id,
            category.Id,
            "Annonce signalée",
            "Annonce destinée au traitement de signalement.",
            ListingType.Service,
            ReferenceDate,
            ReferenceDate.AddDays(30));
        var report = Report.Create(
            reporter.Id,
            ReportTarget.ForListing(listing.Id),
            "Contenu à relire",
            ReferenceDate.AddHours(2),
            "Signalement de test.");
        dbContext.UserProfiles.AddRange(moderator, reporter, owner);
        dbContext.ListingCategories.Add(category);
        dbContext.Listings.Add(listing);
        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync();

        var result = await backOfficeService.ResolveReportAsync(new ReviewBackOfficeReportCommand(
            new BackOfficeActor(moderator.Id, IsAdmin: false, IsModerator: true),
            report.Id,
            "Signalement traité par test."));

        dbContext.ChangeTracker.Clear();
        var persistedReport = await dbContext.Reports.SingleAsync(value => value.Id == report.Id);
        Assert.True(result.Succeeded);
        Assert.Equal(ReportStatus.Resolved, persistedReport.Status);
        Assert.Equal(moderator.Id, persistedReport.ReviewedById);
    }

    private static ServiceProvider CreateAuthorizationServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options => options.AddPrivateListingsBackOfficePolicies());
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal CreatePrincipal(string roleName)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
                new Claim(ClaimTypes.Role, roleName)
            ],
            authenticationType: "Tests");

        return new ClaimsPrincipal(identity);
    }

    private static void AssertPolicy<TPage>(string expectedPolicy)
    {
        var authorizeAttribute = Assert.Single(
            typeof(TPage).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(expectedPolicy, authorizeAttribute.Policy);
    }

    private static async Task InitializeIdentityAsync(IServiceProvider serviceProvider)
        => await serviceProvider.GetRequiredService<PrivateListingsIdentitySeeder>().SeedAsync();

    private static async Task<PrivateListingsUser> CreateIdentityUserAsync(
        PrivateListingsDbContext dbContext,
        UserManager<PrivateListingsUser> userManager,
        string email,
        string displayName,
        string roleName)
    {
        var profile = CreateUser(email, displayName);
        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        var user = new PrivateListingsUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            UserProfileId = profile.Id,
            DisplayName = displayName,
            CreatedAt = ReferenceDate,
            MfaRequiredForAdmin = roleName == PrivateListingsRoleNames.Admin
        };

        var createResult = await userManager.CreateAsync(user, ValidPassword);
        Assert.True(createResult.Succeeded, FormatIdentityErrors(createResult));

        var roleResult = await userManager.AddToRoleAsync(user, roleName);
        Assert.True(roleResult.Succeeded, FormatIdentityErrors(roleResult));

        return user;
    }

    private static UserProfile CreateUser(string email, string displayName)
        => UserProfile.Create(
            EmailAddress.Create(email),
            displayName,
            ReferenceDate,
            "Profil fictif créé par un test de back-office.");

    private static string FormatIdentityErrors(IdentityResult result)
        => string.Join(", ", result.Errors.Select(error => error.Description));
}
