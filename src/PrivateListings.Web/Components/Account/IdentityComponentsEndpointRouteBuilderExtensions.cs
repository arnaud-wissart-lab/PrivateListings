using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrivateListings.Infrastructure.Identity;

namespace Microsoft.AspNetCore.Routing;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/account");

        accountGroup.MapPost("/logout", async (
            ClaimsPrincipal user,
            HttpContext context,
            IAntiforgery antiforgery,
            [FromServices] SignInManager<PrivateListingsUser> signInManager,
            [FromForm] string? returnUrl) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest();
            }

            await signInManager.SignOutAsync().ConfigureAwait(false);
            return Results.LocalRedirect($"~/{returnUrl ?? string.Empty}");
        })
        .DisableAntiforgery()
        .RequireAuthorization();

        return accountGroup;
    }
}
