using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Options;
using PrivateListings.Application.Listings;
using PrivateListings.Domain.Common;

namespace PrivateListings.Application.Tests;

public sealed class ListingRateLimiterTests
{
    [Fact]
    public async Task TryAcquireAsyncBloqueApresLaLimite()
    {
        var limiter = new InMemoryListingRateLimiter(
            Options.Create(new ListingRateLimitOptions
            {
                CreatePermitLimit = 1,
                Window = TimeSpan.FromMinutes(5),
                BlockDuration = TimeSpan.FromMinutes(3)
            }),
            new MutableTimeProvider());
        var request = new ListingRateLimitRequest(
            ListingRateLimitAction.Create,
            UserProfileId.New(),
            new ListingRequestContext("127.0.0.1", "PrivateListings.Tests"));

        Assert.True((await limiter.TryAcquireAsync(request)).IsAllowed);

        var denied = await limiter.TryAcquireAsync(request);

        Assert.False(denied.IsAllowed);
        Assert.True(denied.RetryAfter >= TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task CleInterneNeContientPasIdentifiantsBruts()
    {
        var limiter = new InMemoryListingRateLimiter(
            Options.Create(new ListingRateLimitOptions()),
            new MutableTimeProvider());
        var actorId = UserProfileId.New();
        const string remoteIpAddress = "127.0.0.1";

        await limiter.TryAcquireAsync(new ListingRateLimitRequest(
            ListingRateLimitAction.UploadImage,
            actorId,
            new ListingRequestContext(remoteIpAddress, "PrivateListings.Tests")));

        var bucketsField = typeof(InMemoryListingRateLimiter).GetField(
            "buckets",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var buckets = Assert.IsAssignableFrom<IDictionary>(bucketsField?.GetValue(limiter));
        var key = Assert.Single(buckets.Keys.Cast<string>());

        Assert.Equal(64, key.Length);
        Assert.DoesNotContain(actorId.Value.ToString("N"), key, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(remoteIpAddress, key, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow = new(2026, 7, 7, 8, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
