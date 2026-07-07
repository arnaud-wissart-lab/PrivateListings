using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace PrivateListings.Application.Listings;

public sealed class InMemoryListingRateLimiter(
    IOptions<ListingRateLimitOptions> options,
    TimeProvider timeProvider)
    : IListingRateLimiter
{
    private readonly object gate = new();
    private readonly Dictionary<string, RateLimitBucket> buckets = new(StringComparer.Ordinal);

    public Task<ListingRateLimitLease> TryAcquireAsync(
        ListingRateLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = timeProvider.GetUtcNow();
        var key = BuildKey(request);

        lock (gate)
        {
            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new RateLimitBucket();
                buckets[key] = bucket;
            }

            if (bucket.BlockedUntil is { } blockedUntil && blockedUntil > now)
            {
                return Task.FromResult(new ListingRateLimitLease(false, blockedUntil - now));
            }

            bucket.BlockedUntil = null;
            bucket.Attempts.RemoveAll(attempt => attempt <= now - options.Value.Window);
            bucket.Attempts.Add(now);

            if (bucket.Attempts.Count <= GetPermitLimit(request.Action))
            {
                return Task.FromResult(new ListingRateLimitLease(true));
            }

            bucket.BlockedUntil = now + options.Value.BlockDuration;
            return Task.FromResult(new ListingRateLimitLease(false, options.Value.BlockDuration));
        }
    }

    private int GetPermitLimit(ListingRateLimitAction action)
        => action switch
        {
            ListingRateLimitAction.Create => options.Value.CreatePermitLimit,
            ListingRateLimitAction.UploadImage => options.Value.UploadPermitLimit,
            _ => options.Value.CreatePermitLimit
        };

    private static string BuildKey(ListingRateLimitRequest request)
    {
        var rawKey = string.Join(
            ':',
            request.Action,
            request.ActorId.Value.ToString("N"),
            Normalize(request.RequestContext.RemoteIpAddress));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(hash);
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToLowerInvariant();

    private sealed class RateLimitBucket
    {
        public List<DateTimeOffset> Attempts { get; } = [];

        public DateTimeOffset? BlockedUntil { get; set; }
    }
}
