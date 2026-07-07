using Microsoft.Extensions.Options;
using PrivateListings.Application.Authentication;

namespace PrivateListings.Application.Tests;

public sealed class AccountRateLimiterTests
{
    [Fact]
    public async Task TryAcquireAsyncBloqueApresLaLimite()
    {
        var timeProvider = new MutableTimeProvider();
        var limiter = new InMemoryAccountRateLimiter(
            Options.Create(new AccountRateLimitOptions
            {
                LoginPermitLimit = 2,
                Window = TimeSpan.FromMinutes(5),
                BlockDuration = TimeSpan.FromMinutes(3)
            }),
            timeProvider);
        var request = new AccountRateLimitRequest(AccountRateLimitAction.Login, "test@example.test", "127.0.0.1");

        Assert.True((await limiter.TryAcquireAsync(request)).IsAllowed);
        Assert.True((await limiter.TryAcquireAsync(request)).IsAllowed);

        var denied = await limiter.TryAcquireAsync(request);

        Assert.False(denied.IsAllowed);
        Assert.True(denied.RetryAfter >= TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task ResetAsyncAutoriseUneNouvelleTentative()
    {
        var limiter = new InMemoryAccountRateLimiter(
            Options.Create(new AccountRateLimitOptions
            {
                LoginPermitLimit = 1
            }),
            new MutableTimeProvider());
        var request = new AccountRateLimitRequest(AccountRateLimitAction.Login, "test@example.test", "127.0.0.1");

        Assert.True((await limiter.TryAcquireAsync(request)).IsAllowed);
        Assert.False((await limiter.TryAcquireAsync(request)).IsAllowed);

        await limiter.ResetAsync(request);

        Assert.True((await limiter.TryAcquireAsync(request)).IsAllowed);
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset utcNow = new(2026, 7, 7, 8, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
