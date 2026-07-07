using Microsoft.AspNetCore.Identity;

namespace PrivateListings.Infrastructure.Identity;

public sealed class PrivateListingsRole : IdentityRole<Guid>
{
    public PrivateListingsRole()
    {
    }

    public PrivateListingsRole(string roleName)
        : base(roleName)
    {
    }
}
