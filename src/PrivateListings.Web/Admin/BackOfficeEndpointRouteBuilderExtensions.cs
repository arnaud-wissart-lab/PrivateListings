using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrivateListings.Application.BackOffice;
using PrivateListings.Domain.Common;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Web.Components.Account;

namespace PrivateListings.Web.Admin;

internal static class BackOfficeEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapBackOfficeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var moderation = endpoints.MapGroup("/admin/actions")
            .RequireAuthorization(BackOfficePolicies.Moderation);

        moderation.MapPost("/listings/{listingId:guid}/suspend", SuspendListingAsync);
        moderation.MapPost("/reports/{reportId:guid}/review", MarkReportUnderReviewAsync);
        moderation.MapPost("/reports/{reportId:guid}/resolve", ResolveReportAsync);
        moderation.MapPost("/reports/{reportId:guid}/reject", RejectReportAsync);

        var admin = endpoints.MapGroup("/admin/actions")
            .RequireAuthorization(BackOfficePolicies.Admin);

        admin.MapPost("/users/{userId:guid}/suspend", SuspendUserAsync);
        admin.MapPost("/users/{userId:guid}/reactivate", ReactivateUserAsync);
        admin.MapPost("/users/{userId:guid}/role", ChangeUserRoleAsync);
        admin.MapPost("/listings/{listingId:guid}/delete", DeleteListingAsync);
        admin.MapPost("/categories", CreateCategoryAsync);
        admin.MapPost("/categories/{categoryId:guid}/update", UpdateCategoryAsync);
        admin.MapPost("/categories/{categoryId:guid}/activate", ActivateCategoryAsync);
        admin.MapPost("/categories/{categoryId:guid}/deactivate", DeactivateCategoryAsync);
        admin.MapPost("/demo-seed", RunDemoSeedAsync);

        return endpoints;
    }

    private static async Task<IResult> SuspendUserAsync(
        Guid userId,
        [FromForm] string reason,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        if (!IsConfirmed(confirm))
        {
            return RedirectWithStatus(context, returnUrl, $"/admin/utilisateurs/{userId:D}", "Erreur : confirmation obligatoire.");
        }

        var actor = await ReadActorAsync(context, userManager).ConfigureAwait(false);
        if (actor is null)
        {
            return Results.Challenge();
        }

        var result = await backOfficeService.SuspendUserAsync(
            new SuspendBackOfficeUserCommand(actor, userId, reason),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            returnUrl,
            $"/admin/utilisateurs/{userId:D}",
            FormatResult(result, "Utilisateur suspendu."));
    }

    private static async Task<IResult> ReactivateUserAsync(
        Guid userId,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        if (!IsConfirmed(confirm))
        {
            return RedirectWithStatus(context, returnUrl, $"/admin/utilisateurs/{userId:D}", "Erreur : confirmation obligatoire.");
        }

        var actor = await ReadActorAsync(context, userManager).ConfigureAwait(false);
        if (actor is null)
        {
            return Results.Challenge();
        }

        var result = await backOfficeService.ReactivateUserAsync(
            new ReactivateBackOfficeUserCommand(actor, userId),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            returnUrl,
            $"/admin/utilisateurs/{userId:D}",
            FormatResult(result, "Utilisateur réactivé."));
    }

    private static async Task<IResult> ChangeUserRoleAsync(
        Guid userId,
        [FromForm] string roleName,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        if (!IsConfirmed(confirm))
        {
            return RedirectWithStatus(context, returnUrl, $"/admin/utilisateurs/{userId:D}", "Erreur : confirmation obligatoire.");
        }

        var actor = await ReadActorAsync(context, userManager).ConfigureAwait(false);
        if (actor is null)
        {
            return Results.Challenge();
        }

        var result = await backOfficeService.ChangeUserRoleAsync(
            new ChangeBackOfficeUserRoleCommand(actor, userId, roleName),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            returnUrl,
            $"/admin/utilisateurs/{userId:D}",
            FormatResult(result, "Rôle mis à jour."));
    }

    private static async Task<IResult> SuspendListingAsync(
        Guid listingId,
        [FromForm] string reason,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        if (!IsConfirmed(confirm))
        {
            return RedirectWithStatus(context, returnUrl, "/admin/annonces", "Erreur : confirmation obligatoire.");
        }

        var actor = await ReadActorAsync(context, userManager).ConfigureAwait(false);
        if (actor is null)
        {
            return Results.Challenge();
        }

        var result = await backOfficeService.SuspendListingAsync(
            new SuspendBackOfficeListingCommand(actor, ListingId.From(listingId), reason),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            returnUrl,
            "/admin/annonces",
            FormatResult(result, "Annonce suspendue."));
    }

    private static async Task<IResult> DeleteListingAsync(
        Guid listingId,
        [FromForm] string? reason,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        if (!IsConfirmed(confirm))
        {
            return RedirectWithStatus(context, returnUrl, "/admin/annonces", "Erreur : confirmation obligatoire.");
        }

        var actor = await ReadActorAsync(context, userManager).ConfigureAwait(false);
        if (actor is null)
        {
            return Results.Challenge();
        }

        var result = await backOfficeService.DeleteListingAsync(
            new DeleteBackOfficeListingCommand(actor, ListingId.From(listingId), reason ?? string.Empty),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            returnUrl,
            "/admin/annonces",
            FormatResult(result, "Annonce supprimée logiquement."));
    }

    private static async Task<IResult> MarkReportUnderReviewAsync(
        Guid reportId,
        [FromForm] string? note,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
        => await UpdateReportAsync(
            reportId,
            note,
            confirm,
            returnUrl,
            context,
            antiforgery,
            backOfficeService,
            userManager,
            static (service, command, token) => service.MarkReportUnderReviewAsync(command, token),
            "Signalement pris en revue.",
            cancellationToken).ConfigureAwait(false);

    private static async Task<IResult> ResolveReportAsync(
        Guid reportId,
        [FromForm] string note,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
        => await UpdateReportAsync(
            reportId,
            note,
            confirm,
            returnUrl,
            context,
            antiforgery,
            backOfficeService,
            userManager,
            static (service, command, token) => service.ResolveReportAsync(command, token),
            "Signalement résolu.",
            cancellationToken).ConfigureAwait(false);

    private static async Task<IResult> RejectReportAsync(
        Guid reportId,
        [FromForm] string note,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
        => await UpdateReportAsync(
            reportId,
            note,
            confirm,
            returnUrl,
            context,
            antiforgery,
            backOfficeService,
            userManager,
            static (service, command, token) => service.RejectReportAsync(command, token),
            "Signalement rejeté.",
            cancellationToken).ConfigureAwait(false);

    private static async Task<IResult> CreateCategoryAsync(
        [FromForm] string name,
        [FromForm] string? description,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        var actor = await ReadActorAsync(context, userManager).ConfigureAwait(false);
        if (actor is null)
        {
            return Results.Challenge();
        }

        var result = await backOfficeService.CreateCategoryAsync(
            new CreateBackOfficeCategoryCommand(actor, name, description),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            returnUrl,
            "/admin/categories",
            FormatResult(result, "Catégorie créée."));
    }

    private static async Task<IResult> UpdateCategoryAsync(
        Guid categoryId,
        [FromForm] string name,
        [FromForm] string? description,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        if (!IsConfirmed(confirm))
        {
            return RedirectWithStatus(context, returnUrl, "/admin/categories", "Erreur : confirmation obligatoire.");
        }

        var actor = await ReadActorAsync(context, userManager).ConfigureAwait(false);
        if (actor is null)
        {
            return Results.Challenge();
        }

        var result = await backOfficeService.UpdateCategoryAsync(
            new UpdateBackOfficeCategoryCommand(actor, ListingCategoryId.From(categoryId), name, description),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            returnUrl,
            "/admin/categories",
            FormatResult(result, "Catégorie mise à jour."));
    }

    private static async Task<IResult> ActivateCategoryAsync(
        Guid categoryId,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
        => await ToggleCategoryAsync(
            categoryId,
            confirm,
            returnUrl,
            context,
            antiforgery,
            backOfficeService,
            userManager,
            static (service, command, token) => service.ActivateCategoryAsync(command, token),
            "Catégorie activée.",
            cancellationToken).ConfigureAwait(false);

    private static async Task<IResult> DeactivateCategoryAsync(
        Guid categoryId,
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
        => await ToggleCategoryAsync(
            categoryId,
            confirm,
            returnUrl,
            context,
            antiforgery,
            backOfficeService,
            userManager,
            static (service, command, token) => service.DeactivateCategoryAsync(command, token),
            "Catégorie désactivée.",
            cancellationToken).ConfigureAwait(false);

    private static async Task<IResult> RunDemoSeedAsync(
        [FromForm] string? confirm,
        [FromForm] string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        if (!IsConfirmed(confirm))
        {
            return RedirectWithStatus(context, returnUrl, "/admin", "Erreur : confirmation obligatoire.");
        }

        var actor = await ReadActorAsync(context, userManager).ConfigureAwait(false);
        if (actor is null)
        {
            return Results.Challenge();
        }

        var result = await backOfficeService.RunDemoSeedAsync(
            new RunBackOfficeDemoSeedCommand(actor),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            returnUrl,
            "/admin",
            FormatResult(result, "Seed de démonstration exécuté."));
    }

    private static async Task<IResult> UpdateReportAsync(
        Guid reportId,
        string? note,
        string? confirm,
        string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        Func<IBackOfficeService, ReviewBackOfficeReportCommand, CancellationToken, Task<BackOfficeOperationResult>> update,
        string successMessage,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        if (!IsConfirmed(confirm))
        {
            return RedirectWithStatus(context, returnUrl, "/admin/signalements", "Erreur : confirmation obligatoire.");
        }

        var actor = await ReadActorAsync(context, userManager).ConfigureAwait(false);
        if (actor is null)
        {
            return Results.Challenge();
        }

        var result = await update(
            backOfficeService,
            new ReviewBackOfficeReportCommand(actor, ReportId.From(reportId), note ?? "Prise en revue."),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            returnUrl,
            "/admin/signalements",
            FormatResult(result, successMessage));
    }

    private static async Task<IResult> ToggleCategoryAsync(
        Guid categoryId,
        string? confirm,
        string? returnUrl,
        HttpContext context,
        IAntiforgery antiforgery,
        IBackOfficeService backOfficeService,
        UserManager<PrivateListingsUser> userManager,
        Func<IBackOfficeService, ToggleBackOfficeCategoryCommand, CancellationToken, Task<BackOfficeOperationResult>> update,
        string successMessage,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        if (!IsConfirmed(confirm))
        {
            return RedirectWithStatus(context, returnUrl, "/admin/categories", "Erreur : confirmation obligatoire.");
        }

        var actor = await ReadActorAsync(context, userManager).ConfigureAwait(false);
        if (actor is null)
        {
            return Results.Challenge();
        }

        var result = await update(
            backOfficeService,
            new ToggleBackOfficeCategoryCommand(actor, ListingCategoryId.From(categoryId)),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            returnUrl,
            "/admin/categories",
            FormatResult(result, successMessage));
    }

    private static async Task<BackOfficeActor?> ReadActorAsync(
        HttpContext context,
        UserManager<PrivateListingsUser> userManager)
    {
        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        return new BackOfficeActor(
            user.UserProfileId,
            context.User.IsInRole(PrivateListingsRoleNames.Admin),
            context.User.IsInRole(PrivateListingsRoleNames.Moderator));
    }

    private static string FormatResult(BackOfficeOperationResult result, string successMessage)
        => result.Succeeded
            ? successMessage
            : $"Erreur : {string.Join(" ", result.Errors ?? ["l'opération n'a pas pu aboutir."])}";

    private static bool IsConfirmed(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static IResult RedirectWithStatus(
        HttpContext context,
        string? returnUrl,
        string fallbackUri,
        string message)
    {
        context.Response.Cookies.Append(
            IdentityRedirectManager.StatusCookieName,
            message,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                MaxAge = TimeSpan.FromSeconds(10),
                SameSite = SameSiteMode.Strict,
                Secure = true
            });

        return Results.Redirect(SafeReturnUrl(returnUrl, fallbackUri));
    }

    private static string SafeReturnUrl(string? returnUrl, string fallbackUri)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
            || returnUrl[0] != '/'
            || returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return fallbackUri;
        }

        return returnUrl;
    }
}
