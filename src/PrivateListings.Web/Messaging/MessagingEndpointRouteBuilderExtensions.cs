using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrivateListings.Application.Messaging;
using PrivateListings.Domain.Common;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Web.Components.Account;
using PrivateListings.Web.PublicUrls;

namespace PrivateListings.Web.Messaging;

internal static class MessagingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMessagingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/annonces/{listingId:guid}/repondre", StartListingConversationAsync)
            .RequireAuthorization();

        var group = endpoints.MapGroup("/messagerie").RequireAuthorization();
        group.MapPost("/{conversationId:guid}/messages", SendMessageAsync);
        group.MapPost("/{conversationId:guid}/lue", MarkReadAsync);
        group.MapPost("/{conversationId:guid}/non-lue", MarkUnreadAsync);
        group.MapPost("/{conversationId:guid}/signaler", ReportConversationAsync);
        group.MapPost("/{conversationId:guid}/messages/{messageId:guid}/signaler", ReportMessageAsync);
        group.MapPost("/{conversationId:guid}/bloquer/{blockedUserId:guid}", BlockUserAsync);

        return endpoints;
    }

    private static async Task<IResult> StartListingConversationAsync(
        Guid listingId,
        [FromForm] string body,
        HttpContext context,
        IAntiforgery antiforgery,
        IMessagingService messagingService,
        IPublicUrlBuilder publicUrlBuilder,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);

        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        var result = await messagingService
            .StartListingConversationAsync(
                new StartListingConversationCommand(
                    user.UserProfileId,
                    ListingId.From(listingId),
                    body,
                    BuildRequestContext(context, publicUrlBuilder)),
                cancellationToken)
            .ConfigureAwait(false);

        var redirectUri = result.Succeeded && result.ConversationId.HasValue
            ? $"/messagerie/{result.ConversationId.Value.Value:D}"
            : $"/annonces/{listingId:D}";

        return RedirectWithStatus(context, redirectUri, FormatResult(result, "Message envoyé."));
    }

    private static async Task<IResult> SendMessageAsync(
        Guid conversationId,
        [FromForm] string body,
        HttpContext context,
        IAntiforgery antiforgery,
        IMessagingService messagingService,
        IPublicUrlBuilder publicUrlBuilder,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);

        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        var result = await messagingService
            .SendMessageAsync(
                new SendConversationMessageCommand(
                    user.UserProfileId,
                    ConversationId.From(conversationId),
                    body,
                    BuildRequestContext(context, publicUrlBuilder)),
                cancellationToken)
            .ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            $"/messagerie/{conversationId:D}",
            FormatResult(result, "Message envoyé."));
    }

    private static Task<IResult> MarkReadAsync(
        Guid conversationId,
        HttpContext context,
        IAntiforgery antiforgery,
        IMessagingService messagingService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
        => UpdateReadStateAsync(
            conversationId,
            context,
            antiforgery,
            messagingService,
            userManager,
            markAsRead: true,
            cancellationToken);

    private static Task<IResult> MarkUnreadAsync(
        Guid conversationId,
        HttpContext context,
        IAntiforgery antiforgery,
        IMessagingService messagingService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
        => UpdateReadStateAsync(
            conversationId,
            context,
            antiforgery,
            messagingService,
            userManager,
            markAsRead: false,
            cancellationToken);

    private static async Task<IResult> UpdateReadStateAsync(
        Guid conversationId,
        HttpContext context,
        IAntiforgery antiforgery,
        IMessagingService messagingService,
        UserManager<PrivateListingsUser> userManager,
        bool markAsRead,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);

        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        var command = new ConversationParticipantCommand(user.UserProfileId, ConversationId.From(conversationId));
        var result = markAsRead
            ? await messagingService.MarkReadAsync(command, cancellationToken).ConfigureAwait(false)
            : await messagingService.MarkUnreadAsync(command, cancellationToken).ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            $"/messagerie/{conversationId:D}",
            FormatResult(result, markAsRead ? "Conversation marquée comme lue." : "Conversation marquée comme non lue."));
    }

    private static async Task<IResult> ReportConversationAsync(
        Guid conversationId,
        [FromForm] string reason,
        [FromForm] string? details,
        HttpContext context,
        IAntiforgery antiforgery,
        IMessagingService messagingService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);

        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        var result = await messagingService
            .ReportConversationAsync(
                new ReportConversationCommand(
                    user.UserProfileId,
                    ConversationId.From(conversationId),
                    reason,
                    details),
                cancellationToken)
            .ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            $"/messagerie/{conversationId:D}",
            FormatResult(result, "Signalement enregistré."));
    }

    private static async Task<IResult> ReportMessageAsync(
        Guid conversationId,
        Guid messageId,
        [FromForm] string reason,
        [FromForm] string? details,
        HttpContext context,
        IAntiforgery antiforgery,
        IMessagingService messagingService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);

        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        var result = await messagingService
            .ReportMessageAsync(
                new ReportMessageCommand(
                    user.UserProfileId,
                    ConversationId.From(conversationId),
                    MessageId.From(messageId),
                    reason,
                    details),
                cancellationToken)
            .ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            $"/messagerie/{conversationId:D}",
            FormatResult(result, "Signalement enregistré."));
    }

    private static async Task<IResult> BlockUserAsync(
        Guid conversationId,
        Guid blockedUserId,
        HttpContext context,
        IAntiforgery antiforgery,
        IMessagingService messagingService,
        UserManager<PrivateListingsUser> userManager,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);

        var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
        if (user is null)
        {
            return Results.Challenge();
        }

        var result = await messagingService
            .BlockUserAsync(
                new BlockUserCommand(
                    user.UserProfileId,
                    ConversationId.From(conversationId),
                    UserProfileId.From(blockedUserId)),
                cancellationToken)
            .ConfigureAwait(false);

        return RedirectWithStatus(
            context,
            $"/messagerie/{conversationId:D}",
            FormatResult(result, "Utilisateur bloqué."));
    }

    private static MessagingRequestContext BuildRequestContext(
        HttpContext context,
        IPublicUrlBuilder publicUrlBuilder)
        => new(
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers["User-Agent"].ToString(),
            publicUrlBuilder.BuildAbsoluteUrl("/"));

    private static string FormatResult(MessagingOperationResult result, string successMessage)
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
