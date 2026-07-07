using Microsoft.AspNetCore.Authorization;
using PrivateListings.Infrastructure.Identity;

namespace PrivateListings.Web.Admin;

public static class BackOfficeAuthorizationOptionsExtensions
{
    public static void AddPrivateListingsBackOfficePolicies(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(
            BackOfficePolicies.Admin,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(PrivateListingsRoleNames.Admin));

        options.AddPolicy(
            BackOfficePolicies.Moderation,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(PrivateListingsRoleNames.Admin, PrivateListingsRoleNames.Moderator));
    }
}
