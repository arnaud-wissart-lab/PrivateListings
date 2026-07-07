using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PrivateListings.Application.Listings;
using PrivateListings.Domain.Common;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Web.Components.Account;

namespace PrivateListings.Web.Listings;

internal static class ListingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapListingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/annonces/images/{imageId:guid}", OpenImageAsync);

        var group = endpoints.MapGroup("/annonces").RequireAuthorization();
        group.MapPost("/{listingId:guid}/images", UploadImageAsync);
        group.MapPost("/{listingId:guid}/publier", PublishAsync);
        group.MapPost("/{listingId:guid}/brouillon", MoveToDraftAsync);
        group.MapPost("/{listingId:guid}/supprimer", DeleteAsync);
        group.MapPost("/{listingId:guid}/suspendre", SuspendAsync);

        return endpoints;
    }

    private static async Task<IResult> OpenImageAsync(
        Guid imageId,
        HttpContext context,
        IListingService listingService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        var user = context.User.Identity?.IsAuthenticated == true
            ? await userManager.GetUserAsync(context.User).ConfigureAwait(false)
            : null;

        var image = await listingService.OpenImageAsync(
            ListingImageId.From(imageId),
            user?.UserProfileId,
            CanModerate(context),
            cancellationToken).ConfigureAwait(false);

        return image is null
            ? Results.NotFound()
            : Results.File(
                image.Content,
                image.ContentType,
                lastModified: image.UploadedAt,
                enableRangeProcessing: true);
    }

    private static async Task<IResult> UploadImageAsync(
        Guid listingId,
        [FromForm] IFormFile image,
        [FromForm] string? altText,
        HttpContext context,
        IAntiforgery antiforgery,
        IListingService listingService,
        UserManager<PrivateListingsUser> userManager,
        IOptions<ListingImageOptions> imageOptions,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);

        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        if (image.Length <= 0 || image.Length > imageOptions.Value.MaxSizeInBytes)
        {
            return RedirectWithStatus(
                context,
                $"/annonces/{listingId:D}/modifier",
                "Erreur : la taille du fichier image est invalide.");
        }

        await using var stream = image.OpenReadStream();
        var result = await listingService.UploadImageAsync(
            new UploadListingImageCommand(
                user.UserProfileId,
                ListingId.From(listingId),
                image.FileName,
                image.ContentType,
                stream,
                image.Length,
                altText,
                BuildRequestContext(context)),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            $"/annonces/{listingId:D}/modifier",
            FormatResult(result, "Image ajoutée."));
    }

    private static async Task<IResult> PublishAsync(
        Guid listingId,
        HttpContext context,
        IAntiforgery antiforgery,
        IListingService listingService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        var result = await listingService.PublishAsync(
            ListingId.From(listingId),
            user.UserProfileId,
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            $"/annonces/{listingId:D}/modifier",
            FormatResult(result, "Annonce publiée."));
    }

    private static async Task<IResult> MoveToDraftAsync(
        Guid listingId,
        HttpContext context,
        IAntiforgery antiforgery,
        IListingService listingService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        var result = await listingService.MoveToDraftAsync(
            ListingId.From(listingId),
            user.UserProfileId,
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            $"/annonces/{listingId:D}/modifier",
            FormatResult(result, "Annonce repassée en brouillon."));
    }

    private static async Task<IResult> DeleteAsync(
        Guid listingId,
        HttpContext context,
        IAntiforgery antiforgery,
        IListingService listingService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        var result = await listingService.DeleteAsync(
            ListingId.From(listingId),
            user.UserProfileId,
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            result.Succeeded ? "/mes-annonces" : $"/annonces/{listingId:D}/modifier",
            FormatResult(result, "Annonce supprimée."));
    }

    private static async Task<IResult> SuspendAsync(
        Guid listingId,
        [FromForm] string reason,
        HttpContext context,
        IAntiforgery antiforgery,
        IListingService listingService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        var result = await listingService.SuspendAsync(
            new SuspendListingCommand(
                user.UserProfileId,
                ListingId.From(listingId),
                reason,
                CanModerate(context)),
            cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            $"/annonces/{listingId:D}",
            FormatResult(result, "Annonce suspendue."));
    }

    private static bool CanModerate(HttpContext context)
        => context.User.IsInRole(PrivateListingsRoleNames.Admin)
            || context.User.IsInRole(PrivateListingsRoleNames.Moderator);

    private static ListingRequestContext BuildRequestContext(HttpContext context)
        => new(
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers["User-Agent"].ToString());

    private static string FormatResult(ListingOperationResult result, string successMessage)
        => result.Succeeded
            ? successMessage
            : $"Erreur : {string.Join(" ", result.Errors ?? ["l'opération n'a pas pu aboutir."])}";

    private static IResult RedirectWithStatus(HttpContext context, string uri, string message)
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

        return Results.Redirect(uri);
    }
}
