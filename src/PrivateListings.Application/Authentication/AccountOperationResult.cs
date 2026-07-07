namespace PrivateListings.Application.Authentication;

public sealed record AccountOperationResult(
    AccountOperationStatus Status,
    Guid? UserId = null,
    TimeSpan? RetryAfter = null,
    IReadOnlyCollection<string>? Errors = null)
{
    public bool Succeeded => Status == AccountOperationStatus.Succeeded;

    public static AccountOperationResult Success(Guid? userId = null)
        => new(AccountOperationStatus.Succeeded, userId, null, []);

    public static AccountOperationResult Failed(params string[] errors)
        => new(AccountOperationStatus.Failed, null, null, errors);

    public static AccountOperationResult RateLimited(TimeSpan? retryAfter)
        => new(AccountOperationStatus.RateLimited, null, retryAfter, []);

    public static AccountOperationResult LockedOut()
        => new(AccountOperationStatus.LockedOut, null, null, []);

    public static AccountOperationResult RequiresTwoFactor(Guid userId)
        => new(AccountOperationStatus.RequiresTwoFactor, userId, null, []);

    public static AccountOperationResult RequiresEmailConfirmation(Guid userId)
        => new(AccountOperationStatus.RequiresEmailConfirmation, userId, null, []);

    public static AccountOperationResult NotFound()
        => new(AccountOperationStatus.NotFound, null, null, []);
}
