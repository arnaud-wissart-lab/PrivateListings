namespace PrivateListings.Application.Authentication;

public sealed record AccountRateLimitLease(bool IsAllowed, TimeSpan? RetryAfter)
{
    public static AccountRateLimitLease Allowed { get; } = new(true, null);

    public static AccountRateLimitLease Denied(TimeSpan retryAfter)
        => new(false, retryAfter);
}
