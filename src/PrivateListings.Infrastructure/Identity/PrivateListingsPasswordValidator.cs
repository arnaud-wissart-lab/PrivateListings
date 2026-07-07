using Microsoft.AspNetCore.Identity;
using PrivateListings.Application.Authentication;

namespace PrivateListings.Infrastructure.Identity;

public sealed class PrivateListingsPasswordValidator(IAccountPasswordPolicy passwordPolicy)
    : IPasswordValidator<PrivateListingsUser>
{
    public Task<IdentityResult> ValidateAsync(
        UserManager<PrivateListingsUser> manager,
        PrivateListingsUser user,
        string? password)
    {
        var result = passwordPolicy.Validate(password);
        if (result.Succeeded)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        var errors = result.Errors.Select(error => new IdentityError
        {
            Code = nameof(PrivateListingsPasswordValidator),
            Description = error
        });

        return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
    }
}
