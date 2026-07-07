using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PrivateListings.Application.BackOffice;
using PrivateListings.Domain.Auditing;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;
using PrivateListings.Domain.Moderation;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Infrastructure.Persistence;
using PrivateListings.Infrastructure.Persistence.Seeding;

namespace PrivateListings.Infrastructure.BackOffice;

public sealed class EfBackOfficeService(
    PrivateListingsDbContext dbContext,
    UserManager<PrivateListingsUser> userManager,
    RoleManager<PrivateListingsRole> roleManager,
    PrivateListingsDemoDataSeeder demoDataSeeder,
    IHostEnvironment environment,
    IOptions<PrivateListingsDemoSeedOptions> demoSeedOptions,
    TimeProvider timeProvider)
    : IBackOfficeService
{
    private const int MaxPageSize = 100;
    private static readonly string[] BackOfficeRoles =
    [
        PrivateListingsRoleNames.Admin,
        PrivateListingsRoleNames.Moderator,
        PrivateListingsRoleNames.Member
    ];

    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BackOfficeDashboard> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var since = timeProvider.GetUtcNow().AddDays(-7);

        var totalUsers = await dbContext.Users.CountAsync(cancellationToken).ConfigureAwait(false);
        var suspendedUsers = await dbContext.Users
            .CountAsync(user => user.SuspendedAt.HasValue, cancellationToken)
            .ConfigureAwait(false);
        var publishedListings = await dbContext.Listings
            .CountAsync(listing => listing.Status == ListingStatus.Published, cancellationToken)
            .ConfigureAwait(false);
        var suspendedListings = await dbContext.Listings
            .CountAsync(listing => listing.Status == ListingStatus.Suspended, cancellationToken)
            .ConfigureAwait(false);
        var openReports = await dbContext.Reports
            .CountAsync(report => report.Status == ReportStatus.Open || report.Status == ReportStatus.UnderReview, cancellationToken)
            .ConfigureAwait(false);
        var recentAuditLogs = await dbContext.AuditLogs
            .CountAsync(auditLog => auditLog.OccurredAt >= since, cancellationToken)
            .ConfigureAwait(false);

        return new BackOfficeDashboard(
            totalUsers,
            suspendedUsers,
            publishedListings,
            suspendedListings,
            openReports,
            recentAuditLogs);
    }

    public async Task<BackOfficePagedResult<BackOfficeUserSummary>> ListUsersAsync(
        BackOfficePagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, skip) = NormalizePaging(query.Page, query.PageSize);
        var users = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = BuildContainsPattern(query.Search);
            users = users.Where(user =>
                EF.Functions.ILike(user.DisplayName, pattern)
                || (user.Email != null && EF.Functions.ILike(user.Email, pattern)));
        }

        var totalCount = await users.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageUsers = await users
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.DisplayName)
            .Skip(skip)
            .Take(pageSize)
            .Select(user => new
            {
                user.Id,
                user.UserProfileId,
                user.DisplayName,
                user.CreatedAt,
                user.SuspendedAt,
                user.EmailConfirmed
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rolesByUserId = await LoadRolesAsync(
            pageUsers.Select(user => user.Id).ToArray(),
            cancellationToken).ConfigureAwait(false);

        var items = pageUsers
            .Select(user => new BackOfficeUserSummary(
                user.Id,
                user.UserProfileId,
                user.DisplayName,
                rolesByUserId.GetValueOrDefault(user.Id, []),
                user.CreatedAt,
                user.SuspendedAt,
                user.EmailConfirmed))
            .ToList();

        return new BackOfficePagedResult<BackOfficeUserSummary>(items, page, pageSize, totalCount);
    }

    public async Task<BackOfficeUserDetails?> GetUserDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString("D")).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var isLockedOut = await userManager.IsLockedOutAsync(user).ConfigureAwait(false);
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user).ConfigureAwait(false);

        var listingsCount = await dbContext.Listings
            .AsNoTracking()
            .CountAsync(listing => listing.OwnerId == user.UserProfileId, cancellationToken)
            .ConfigureAwait(false);
        var openReportsCount = await dbContext.Reports
            .AsNoTracking()
            .CountAsync(
                report => report.Target.Type == ReportTargetType.UserProfile
                    && report.Target.TargetId == user.UserProfileId.Value
                    && (report.Status == ReportStatus.Open || report.Status == ReportStatus.UnderReview),
                cancellationToken)
            .ConfigureAwait(false);
        var auditLogsCount = await dbContext.AuditLogs
            .AsNoTracking()
            .CountAsync(auditLog => auditLog.ActorUserProfileId == user.UserProfileId, cancellationToken)
            .ConfigureAwait(false);

        return new BackOfficeUserDetails(
            user.Id,
            user.UserProfileId,
            user.DisplayName,
            user.Email ?? string.Empty,
            roles.OrderBy(role => role, StringComparer.Ordinal).ToArray(),
            user.CreatedAt,
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            isLockedOut,
            lockoutEnd,
            user.SuspendedAt,
            user.SuspensionReason,
            listingsCount,
            openReportsCount,
            auditLogsCount);
    }

    public async Task<BackOfficeOperationResult> SuspendUserAsync(
        SuspendBackOfficeUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.Actor.IsAdmin)
        {
            return BackOfficeOperationResult.Forbidden("Seul un administrateur peut suspendre un utilisateur.");
        }

        var reason = NormalizeRequired(command.Reason, "Le motif de suspension est obligatoire.", 500);
        if (reason.Errors.Count > 0)
        {
            return BackOfficeOperationResult.ValidationFailed(reason.Errors.ToArray());
        }

        var user = await userManager.FindByIdAsync(command.UserId.ToString("D")).ConfigureAwait(false);
        if (user is null)
        {
            return BackOfficeOperationResult.NotFound("Utilisateur introuvable.");
        }

        if (user.UserProfileId == command.Actor.UserProfileId)
        {
            return BackOfficeOperationResult.ValidationFailed("Un administrateur ne peut pas suspendre son propre compte.");
        }

        var now = timeProvider.GetUtcNow();
        user.SuspendedAt = now;
        user.SuspensionReason = reason.Value;
        user.LockoutEnabled = true;
        user.LockoutEnd = now.AddYears(100);

        var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
        {
            return BackOfficeOperationResult.ValidationFailed(ToPublicErrors(updateResult).ToArray());
        }

        await WriteAuditAsync(
            command.Actor.UserProfileId,
            "User.Suspended",
            AuditLogSubject.ForUserProfile(user.UserProfileId),
            new Dictionary<string, object?>
            {
                ["identityUserId"] = user.Id
            },
            cancellationToken).ConfigureAwait(false);

        return BackOfficeOperationResult.Success();
    }

    public async Task<BackOfficeOperationResult> ReactivateUserAsync(
        ReactivateBackOfficeUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.Actor.IsAdmin)
        {
            return BackOfficeOperationResult.Forbidden("Seul un administrateur peut réactiver un utilisateur.");
        }

        var user = await userManager.FindByIdAsync(command.UserId.ToString("D")).ConfigureAwait(false);
        if (user is null)
        {
            return BackOfficeOperationResult.NotFound("Utilisateur introuvable.");
        }

        user.SuspendedAt = null;
        user.SuspensionReason = null;
        user.LockoutEnd = null;

        var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
        {
            return BackOfficeOperationResult.ValidationFailed(ToPublicErrors(updateResult).ToArray());
        }

        await WriteAuditAsync(
            command.Actor.UserProfileId,
            "User.Reactivated",
            AuditLogSubject.ForUserProfile(user.UserProfileId),
            new Dictionary<string, object?>
            {
                ["identityUserId"] = user.Id
            },
            cancellationToken).ConfigureAwait(false);

        return BackOfficeOperationResult.Success();
    }

    public async Task<BackOfficeOperationResult> ChangeUserRoleAsync(
        ChangeBackOfficeUserRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.Actor.IsAdmin)
        {
            return BackOfficeOperationResult.Forbidden("Seul un administrateur peut modifier un rôle.");
        }

        var roleName = NormalizeRoleName(command.RoleName);
        if (roleName is null)
        {
            return BackOfficeOperationResult.ValidationFailed("Le rôle demandé est invalide.");
        }

        var user = await userManager.FindByIdAsync(command.UserId.ToString("D")).ConfigureAwait(false);
        if (user is null)
        {
            return BackOfficeOperationResult.NotFound("Utilisateur introuvable.");
        }

        if (user.UserProfileId == command.Actor.UserProfileId && roleName != PrivateListingsRoleNames.Admin)
        {
            return BackOfficeOperationResult.ValidationFailed("Un administrateur ne peut pas retirer son propre rôle Admin.");
        }

        if (!await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
        {
            var createRole = await roleManager.CreateAsync(new PrivateListingsRole(roleName)).ConfigureAwait(false);
            if (!createRole.Succeeded)
            {
                return BackOfficeOperationResult.ValidationFailed(ToPublicErrors(createRole).ToArray());
            }
        }

        var currentRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var rolesToRemove = currentRoles
            .Where(role => BackOfficeRoles.Contains(role, StringComparer.Ordinal))
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove).ConfigureAwait(false);
            if (!removeResult.Succeeded)
            {
                return BackOfficeOperationResult.ValidationFailed(ToPublicErrors(removeResult).ToArray());
            }
        }

        var addResult = await userManager.AddToRoleAsync(user, roleName).ConfigureAwait(false);
        if (!addResult.Succeeded)
        {
            return BackOfficeOperationResult.ValidationFailed(ToPublicErrors(addResult).ToArray());
        }

        user.MfaRequiredForAdmin = roleName == PrivateListingsRoleNames.Admin;
        var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
        {
            return BackOfficeOperationResult.ValidationFailed(ToPublicErrors(updateResult).ToArray());
        }

        await WriteAuditAsync(
            command.Actor.UserProfileId,
            "User.RoleChanged",
            AuditLogSubject.ForUserProfile(user.UserProfileId),
            new Dictionary<string, object?>
            {
                ["identityUserId"] = user.Id,
                ["oldRoles"] = rolesToRemove,
                ["newRole"] = roleName
            },
            cancellationToken).ConfigureAwait(false);

        return BackOfficeOperationResult.Success();
    }

    public async Task<BackOfficePagedResult<BackOfficeListingSummary>> ListListingsAsync(
        BackOfficeListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, skip) = NormalizePaging(query.Page, query.PageSize);

        var listings =
            from listing in dbContext.Listings.AsNoTracking()
            join category in dbContext.ListingCategories.AsNoTracking() on listing.CategoryId equals category.Id
            join owner in dbContext.UserProfiles.AsNoTracking() on listing.OwnerId equals owner.Id
            select new
            {
                Listing = listing,
                CategoryName = category.Name,
                OwnerDisplayName = owner.DisplayName
            };

        if (query.Status.HasValue)
        {
            listings = listings.Where(item => item.Listing.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = BuildContainsPattern(query.Search);
            listings = listings.Where(item =>
                EF.Functions.ILike(item.Listing.Title, pattern)
                || EF.Functions.ILike(item.CategoryName, pattern)
                || EF.Functions.ILike(item.OwnerDisplayName, pattern));
        }

        var totalCount = await listings.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await listings
            .OrderByDescending(item => item.Listing.UpdatedAt ?? item.Listing.PublishedAt ?? item.Listing.CreatedAt)
            .ThenBy(item => item.Listing.Title)
            .Skip(skip)
            .Take(pageSize)
            .Select(item => new BackOfficeListingSummary(
                item.Listing.Id,
                item.Listing.Title,
                item.Listing.Status,
                item.Listing.Type,
                item.CategoryName,
                item.OwnerDisplayName,
                item.Listing.CreatedAt,
                item.Listing.PublishedAt,
                item.Listing.SuspendedAt,
                item.Listing.DeletedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new BackOfficePagedResult<BackOfficeListingSummary>(items, page, pageSize, totalCount);
    }

    public async Task<BackOfficeOperationResult> SuspendListingAsync(
        SuspendBackOfficeListingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.Actor.CanModerate)
        {
            return BackOfficeOperationResult.Forbidden("Seuls les administrateurs et modérateurs peuvent suspendre une annonce.");
        }

        var reason = NormalizeRequired(command.Reason, "Le motif de suspension est obligatoire.", 500);
        if (reason.Errors.Count > 0)
        {
            return BackOfficeOperationResult.ValidationFailed(reason.Errors.ToArray());
        }

        var listing = await dbContext.Listings
            .SingleOrDefaultAsync(value => value.Id == command.ListingId, cancellationToken)
            .ConfigureAwait(false);
        if (listing is null)
        {
            return BackOfficeOperationResult.NotFound("Annonce introuvable.");
        }

        try
        {
            listing.Suspend(reason.Value, timeProvider.GetUtcNow());
        }
        catch (ArgumentException exception)
        {
            return BackOfficeOperationResult.ValidationFailed(exception.Message);
        }
        catch (DomainException exception)
        {
            return BackOfficeOperationResult.ValidationFailed(exception.Message);
        }

        await WriteAuditWithoutSaveAsync(
            command.Actor.UserProfileId,
            "Listing.Suspended",
            AuditLogSubject.ForListing(listing.Id),
            new Dictionary<string, object?>
            {
                ["actorRole"] = command.Actor.IsAdmin ? PrivateListingsRoleNames.Admin : PrivateListingsRoleNames.Moderator
            }).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BackOfficeOperationResult.Success();
    }

    public async Task<BackOfficeOperationResult> DeleteListingAsync(
        DeleteBackOfficeListingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.Actor.IsAdmin)
        {
            return BackOfficeOperationResult.Forbidden("Seul un administrateur peut supprimer logiquement une annonce.");
        }

        var listing = await dbContext.Listings
            .SingleOrDefaultAsync(value => value.Id == command.ListingId, cancellationToken)
            .ConfigureAwait(false);
        if (listing is null)
        {
            return BackOfficeOperationResult.NotFound("Annonce introuvable.");
        }

        listing.Delete(timeProvider.GetUtcNow());

        await WriteAuditWithoutSaveAsync(
            command.Actor.UserProfileId,
            "Listing.Deleted",
            AuditLogSubject.ForListing(listing.Id),
            new Dictionary<string, object?>
            {
                ["reasonProvided"] = !string.IsNullOrWhiteSpace(command.Reason)
            }).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BackOfficeOperationResult.Success();
    }

    public async Task<BackOfficePagedResult<BackOfficeReportSummary>> ListReportsAsync(
        BackOfficeReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, skip) = NormalizePaging(query.Page, query.PageSize);

        var reports =
            from report in dbContext.Reports.AsNoTracking()
            join reporter in dbContext.UserProfiles.AsNoTracking() on report.ReporterId equals reporter.Id
            join reviewer in dbContext.UserProfiles.AsNoTracking() on report.ReviewedById equals reviewer.Id into reviewerGroup
            from reviewer in reviewerGroup.DefaultIfEmpty()
            select new
            {
                Report = report,
                ReporterDisplayName = reporter.DisplayName,
                ReviewedByDisplayName = reviewer == null ? null : reviewer.DisplayName
            };

        if (query.Status.HasValue)
        {
            reports = reports.Where(item => item.Report.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = BuildContainsPattern(query.Search);
            reports = reports.Where(item =>
                EF.Functions.ILike(item.Report.Reason, pattern)
                || (item.Report.Details != null && EF.Functions.ILike(item.Report.Details, pattern))
                || EF.Functions.ILike(item.ReporterDisplayName, pattern));
        }

        var totalCount = await reports.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageReports = await reports
            .OrderByDescending(item => item.Report.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = pageReports
            .Select(item => new BackOfficeReportSummary(
                item.Report.Id,
                item.Report.Status,
                item.Report.Target.Type,
                item.Report.Target.TargetId,
                item.ReporterDisplayName,
                item.Report.Reason,
                item.Report.Details,
                item.Report.CreatedAt,
                item.Report.ReviewedAt,
                item.ReviewedByDisplayName,
                item.Report.ResolutionNote,
                null))
            .ToList();

        items = await AddReportTargetLabelsAsync(items, cancellationToken).ConfigureAwait(false);

        return new BackOfficePagedResult<BackOfficeReportSummary>(items, page, pageSize, totalCount);
    }

    public Task<BackOfficeOperationResult> MarkReportUnderReviewAsync(
        ReviewBackOfficeReportCommand command,
        CancellationToken cancellationToken = default)
        => UpdateReportAsync(
            command,
            "Report.MarkedUnderReview",
            (report, actorId, now) => report.MarkUnderReview(actorId, now),
            cancellationToken);

    public Task<BackOfficeOperationResult> ResolveReportAsync(
        ReviewBackOfficeReportCommand command,
        CancellationToken cancellationToken = default)
        => UpdateReportAsync(
            command,
            "Report.Resolved",
            (report, actorId, now) => report.Resolve(actorId, command.Note, now),
            cancellationToken);

    public Task<BackOfficeOperationResult> RejectReportAsync(
        ReviewBackOfficeReportCommand command,
        CancellationToken cancellationToken = default)
        => UpdateReportAsync(
            command,
            "Report.Rejected",
            (report, actorId, now) => report.Reject(actorId, command.Note, now),
            cancellationToken);

    public async Task<BackOfficePagedResult<BackOfficeAuditLogSummary>> ListAuditLogsAsync(
        BackOfficePagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, skip) = NormalizePaging(query.Page, query.PageSize);

        var auditLogs =
            from auditLog in dbContext.AuditLogs.AsNoTracking()
            join actor in dbContext.UserProfiles.AsNoTracking() on auditLog.ActorUserProfileId equals actor.Id into actorGroup
            from actor in actorGroup.DefaultIfEmpty()
            select new
            {
                AuditLog = auditLog,
                ActorDisplayName = actor == null ? null : actor.DisplayName
            };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = BuildContainsPattern(query.Search);
            auditLogs = auditLogs.Where(item =>
                EF.Functions.ILike(item.AuditLog.Action, pattern)
                || (item.AuditLog.Metadata != null && EF.Functions.ILike(item.AuditLog.Metadata, pattern))
                || (item.ActorDisplayName != null && EF.Functions.ILike(item.ActorDisplayName, pattern)));
        }

        var totalCount = await auditLogs.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageAuditLogs = await auditLogs
            .OrderByDescending(item => item.AuditLog.OccurredAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = pageAuditLogs
            .Select(item => new BackOfficeAuditLogSummary(
                item.AuditLog.Id,
                item.AuditLog.Action,
                item.AuditLog.Subject.Type,
                item.AuditLog.Subject.EntityId,
                item.ActorDisplayName,
                item.AuditLog.OccurredAt,
                item.AuditLog.Metadata))
            .ToList();

        return new BackOfficePagedResult<BackOfficeAuditLogSummary>(items, page, pageSize, totalCount);
    }

    public async Task<BackOfficePagedResult<BackOfficeCategorySummary>> ListCategoriesAsync(
        BackOfficePagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, skip) = NormalizePaging(query.Page, query.PageSize);
        var categories = dbContext.ListingCategories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = BuildContainsPattern(query.Search);
            categories = categories.Where(category =>
                EF.Functions.ILike(category.Name, pattern)
                || (category.Description != null && EF.Functions.ILike(category.Description, pattern)));
        }

        var totalCount = await categories.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await categories
            .OrderBy(category => category.Name)
            .Skip(skip)
            .Take(pageSize)
            .Select(category => new BackOfficeCategorySummary(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.CreatedAt,
                dbContext.Listings.Count(listing => listing.CategoryId == category.Id)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new BackOfficePagedResult<BackOfficeCategorySummary>(items, page, pageSize, totalCount);
    }

    public async Task<BackOfficeOperationResult> CreateCategoryAsync(
        CreateBackOfficeCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.Actor.IsAdmin)
        {
            return BackOfficeOperationResult.Forbidden("Seul un administrateur peut gérer les catégories.");
        }

        try
        {
            var category = ListingCategory.Create(
                command.Name,
                timeProvider.GetUtcNow(),
                command.Description);

            dbContext.ListingCategories.Add(category);
            await WriteAuditWithoutSaveAsync(
                command.Actor.UserProfileId,
                "ListingCategory.Created",
                AuditLogSubject.ForListingCategory(category.Id),
                null).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentException exception)
        {
            return BackOfficeOperationResult.ValidationFailed(exception.Message);
        }
        catch (DbUpdateException)
        {
            return BackOfficeOperationResult.ValidationFailed("La catégorie n'a pas pu être créée. Vérifiez que son nom est unique.");
        }

        return BackOfficeOperationResult.Success();
    }

    public async Task<BackOfficeOperationResult> UpdateCategoryAsync(
        UpdateBackOfficeCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.Actor.IsAdmin)
        {
            return BackOfficeOperationResult.Forbidden("Seul un administrateur peut gérer les catégories.");
        }

        var category = await dbContext.ListingCategories
            .SingleOrDefaultAsync(value => value.Id == command.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return BackOfficeOperationResult.NotFound("Catégorie introuvable.");
        }

        try
        {
            category.Rename(command.Name, command.Description);
            await WriteAuditWithoutSaveAsync(
                command.Actor.UserProfileId,
                "ListingCategory.Updated",
                AuditLogSubject.ForListingCategory(category.Id),
                null).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentException exception)
        {
            return BackOfficeOperationResult.ValidationFailed(exception.Message);
        }
        catch (DbUpdateException)
        {
            return BackOfficeOperationResult.ValidationFailed("La catégorie n'a pas pu être mise à jour. Vérifiez que son nom est unique.");
        }

        return BackOfficeOperationResult.Success();
    }

    public Task<BackOfficeOperationResult> ActivateCategoryAsync(
        ToggleBackOfficeCategoryCommand command,
        CancellationToken cancellationToken = default)
        => ToggleCategoryAsync(command, activate: true, cancellationToken);

    public Task<BackOfficeOperationResult> DeactivateCategoryAsync(
        ToggleBackOfficeCategoryCommand command,
        CancellationToken cancellationToken = default)
        => ToggleCategoryAsync(command, activate: false, cancellationToken);

    public async Task<BackOfficeOperationResult> RunDemoSeedAsync(
        RunBackOfficeDemoSeedCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.Actor.IsAdmin)
        {
            return BackOfficeOperationResult.Forbidden("Seul un administrateur peut lancer le seed de démonstration.");
        }

        if (!environment.IsEnvironment("Demo"))
        {
            return BackOfficeOperationResult.Forbidden("Le seed de démonstration ne peut être lancé que dans l'environnement Demo.");
        }

        var options = new PrivateListingsDemoSeedOptions
        {
            Enabled = true,
            Accounts = demoSeedOptions.Value.Accounts
        };

        await demoDataSeeder.SeedAsync(environment, options, cancellationToken).ConfigureAwait(false);
        await WriteAuditAsync(
            command.Actor.UserProfileId,
            "Demo.SeedExecuted",
            AuditLogSubject.ForSystem("DemoSeed"),
            null,
            cancellationToken).ConfigureAwait(false);

        return BackOfficeOperationResult.Success();
    }

    private async Task<BackOfficeOperationResult> UpdateReportAsync(
        ReviewBackOfficeReportCommand command,
        string auditAction,
        Action<Report, UserProfileId, DateTimeOffset> update,
        CancellationToken cancellationToken)
    {
        if (!command.Actor.CanModerate)
        {
            return BackOfficeOperationResult.Forbidden("Seuls les administrateurs et modérateurs peuvent traiter un signalement.");
        }

        var report = await dbContext.Reports
            .SingleOrDefaultAsync(value => value.Id == command.ReportId, cancellationToken)
            .ConfigureAwait(false);
        if (report is null)
        {
            return BackOfficeOperationResult.NotFound("Signalement introuvable.");
        }

        if (!CanHandleReport(command.Actor, report))
        {
            return BackOfficeOperationResult.Forbidden("Ce signalement est réservé à un administrateur.");
        }

        try
        {
            update(report, command.Actor.UserProfileId, timeProvider.GetUtcNow());
        }
        catch (ArgumentException exception)
        {
            return BackOfficeOperationResult.ValidationFailed(exception.Message);
        }
        catch (DomainException exception)
        {
            return BackOfficeOperationResult.ValidationFailed(exception.Message);
        }

        await WriteAuditWithoutSaveAsync(
            command.Actor.UserProfileId,
            auditAction,
            AuditLogSubject.ForReport(report.Id),
            new Dictionary<string, object?>
            {
                ["targetType"] = report.Target.Type.ToString(),
                ["targetId"] = report.Target.TargetId
            }).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BackOfficeOperationResult.Success();
    }

    private async Task<BackOfficeOperationResult> ToggleCategoryAsync(
        ToggleBackOfficeCategoryCommand command,
        bool activate,
        CancellationToken cancellationToken)
    {
        if (!command.Actor.IsAdmin)
        {
            return BackOfficeOperationResult.Forbidden("Seul un administrateur peut gérer les catégories.");
        }

        var category = await dbContext.ListingCategories
            .SingleOrDefaultAsync(value => value.Id == command.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return BackOfficeOperationResult.NotFound("Catégorie introuvable.");
        }

        if (activate)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await WriteAuditWithoutSaveAsync(
            command.Actor.UserProfileId,
            activate ? "ListingCategory.Activated" : "ListingCategory.Deactivated",
            AuditLogSubject.ForListingCategory(category.Id),
            null).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BackOfficeOperationResult.Success();
    }

    private static bool CanHandleReport(BackOfficeActor actor, Report report)
        => actor.IsAdmin || report.Target.Type == ReportTargetType.Listing;

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> LoadRolesAsync(
        Guid[] userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Length == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<string>>();
        }

        var roleRows = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                select new
                {
                    userRole.UserId,
                    RoleName = role.Name ?? string.Empty
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return roleRows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(row => row.RoleName)
                    .Where(role => !string.IsNullOrWhiteSpace(role))
                    .OrderBy(role => role, StringComparer.Ordinal)
                    .ToArray());
    }

    private async Task<List<BackOfficeReportSummary>> AddReportTargetLabelsAsync(
        IReadOnlyList<BackOfficeReportSummary> reports,
        CancellationToken cancellationToken)
    {
        var listingIds = reports
            .Where(report => report.TargetType == ReportTargetType.Listing)
            .Select(report => ListingId.From(report.TargetId))
            .Distinct()
            .ToArray();
        var userProfileIds = reports
            .Where(report => report.TargetType == ReportTargetType.UserProfile)
            .Select(report => UserProfileId.From(report.TargetId))
            .Distinct()
            .ToArray();

        var listingLabels = listingIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Listings
                .AsNoTracking()
                .Where(listing => listingIds.Contains(listing.Id))
                .ToDictionaryAsync(
                    listing => listing.Id.Value,
                    listing => listing.Title,
                    cancellationToken)
                .ConfigureAwait(false);

        var userLabels = userProfileIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.UserProfiles
                .AsNoTracking()
                .Where(userProfile => userProfileIds.Contains(userProfile.Id))
                .ToDictionaryAsync(
                    userProfile => userProfile.Id.Value,
                    userProfile => userProfile.DisplayName,
                    cancellationToken)
                .ConfigureAwait(false);

        return reports
            .Select(report => report with
            {
                TargetLabel = report.TargetType switch
                {
                    ReportTargetType.Listing => listingLabels.GetValueOrDefault(report.TargetId),
                    ReportTargetType.UserProfile => userLabels.GetValueOrDefault(report.TargetId),
                    ReportTargetType.Conversation => "Conversation privée",
                    ReportTargetType.Message => "Message privé",
                    _ => null
                }
            })
            .ToList();
    }

    private async Task WriteAuditAsync(
        UserProfileId actorUserProfileId,
        string action,
        AuditLogSubject subject,
        IDictionary<string, object?>? metadata,
        CancellationToken cancellationToken)
    {
        await WriteAuditWithoutSaveAsync(actorUserProfileId, action, subject, metadata).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task WriteAuditWithoutSaveAsync(
        UserProfileId actorUserProfileId,
        string action,
        AuditLogSubject subject,
        IDictionary<string, object?>? metadata)
    {
        dbContext.AuditLogs.Add(AuditLog.Create(
            actorUserProfileId,
            action,
            subject,
            timeProvider.GetUtcNow(),
            metadata is null ? null : JsonSerializer.Serialize(metadata, MetadataJsonOptions)));

        return Task.CompletedTask;
    }

    private static (int Page, int PageSize, int Skip) NormalizePaging(int page, int pageSize)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        return (normalizedPage, normalizedPageSize, (normalizedPage - 1) * normalizedPageSize);
    }

    private static string BuildContainsPattern(string value)
        => $"%{value.Trim()}%";

    private static NormalizedValue NormalizeRequired(string value, string requiredMessage, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new NormalizedValue(string.Empty, [requiredMessage]);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            return new NormalizedValue(string.Empty, [$"La valeur ne doit pas dépasser {maxLength} caractères."]);
        }

        return new NormalizedValue(normalized, []);
    }

    private static string? NormalizeRoleName(string value)
        => BackOfficeRoles.SingleOrDefault(role => string.Equals(role, value, StringComparison.Ordinal));

    private static IEnumerable<string> ToPublicErrors(IdentityResult result)
        => result.Errors.Select(error => error.Description);

    private sealed record NormalizedValue(string Value, IReadOnlyList<string> Errors);
}
