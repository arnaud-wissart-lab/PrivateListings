namespace PrivateListings.Application.Authentication;

public enum AccountOperationStatus
{
    Succeeded,
    Failed,
    RateLimited,
    LockedOut,
    RequiresTwoFactor,
    RequiresEmailConfirmation,
    NotFound
}
