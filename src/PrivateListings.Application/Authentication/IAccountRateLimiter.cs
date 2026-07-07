namespace PrivateListings.Application.Authentication;

public interface IAccountRateLimiter
{
    ValueTask<AccountRateLimitLease> TryAcquireAsync(
        AccountRateLimitRequest request,
        CancellationToken cancellationToken = default);

    ValueTask ResetAsync(
        AccountRateLimitRequest request,
        CancellationToken cancellationToken = default);
}
