using System.Security.Claims;
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
            [FromServices] SignInManager<PrivateListingsUser> signInManager,
            [FromForm] string? returnUrl) =>
        {
            await signInManager.SignOutAsync().ConfigureAwait(false);
            return TypedResults.LocalRedirect($"~/{returnUrl ?? string.Empty}");
        }).RequireAuthorization();

        return accountGroup;
    }
}
