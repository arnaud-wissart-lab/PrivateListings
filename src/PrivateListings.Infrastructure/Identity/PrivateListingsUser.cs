using Microsoft.AspNetCore.Identity;
using PrivateListings.Domain.Common;

namespace PrivateListings.Infrastructure.Identity;

public sealed class PrivateListingsUser : IdentityUser<Guid>
{
    public UserProfileId UserProfileId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public bool MfaRequiredForAdmin { get; set; }
}
