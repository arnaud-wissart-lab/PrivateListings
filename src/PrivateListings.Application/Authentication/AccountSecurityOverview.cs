namespace PrivateListings.Application.Authentication;

public sealed record AccountSecurityOverview(
    Guid UserId,
    string Email,
    string DisplayName,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool MfaRequiredForAdmin,
    bool IsLockedOut,
    DateTimeOffset? LockoutEnd);
