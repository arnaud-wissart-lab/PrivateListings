using Microsoft.AspNetCore.Identity;
using PrivateListings.Infrastructure.Identity;

namespace PrivateListings.Infrastructure.Persistence.Seeding;

public sealed class PrivateListingsIdentitySeeder(RoleManager<PrivateListingsRole> roleManager)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(PrivateListingsRoleNames.Admin, cancellationToken).ConfigureAwait(false);
        await EnsureRoleAsync(PrivateListingsRoleNames.Moderator, cancellationToken).ConfigureAwait(false);
        await EnsureRoleAsync(PrivateListingsRoleNames.Member, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new PrivateListingsRole(roleName)).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Le rôle Identity '{roleName}' n'a pas pu être créé : {errors}");
        }
    }
}
