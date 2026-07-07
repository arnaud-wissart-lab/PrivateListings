using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace PrivateListings.Application.Messaging;

public sealed class InMemoryMessagingRateLimiter(
    IOptions<MessagingRateLimitOptions> options,
    TimeProvider timeProvider)
    : IMessagingRateLimiter
{
    private readonly ConcurrentDictionary<string, RateLimitBucket> buckets = new();

    public Task<MessagingRateLimitLease> TryAcquireAsync(
        MessagingRateLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = timeProvider.GetUtcNow();
        var policy = options.Value;
        var bucket = buckets.GetOrAdd(BuildKey(request), _ => new RateLimitBucket(now));

        lock (bucket)
        {
            if (bucket.BlockedUntil is { } blockedUntil && blockedUntil > now)
            {
                return Task.FromResult(new MessagingRateLimitLease(false, blockedUntil - now));
            }

            if (now - bucket.WindowStartedAt >= policy.Window)
            {
                bucket.WindowStartedAt = now;
                bucket.AttemptCount = 0;
                bucket.BlockedUntil = null;
            }

            bucket.AttemptCount++;

            if (bucket.AttemptCount <= policy.SendPermitLimit)
            {
                return Task.FromResult(new MessagingRateLimitLease(true));
            }

            bucket.BlockedUntil = now.Add(policy.BlockDuration);
            return Task.FromResult(new MessagingRateLimitLease(false, policy.BlockDuration));
        }
    }

    private static string BuildKey(MessagingRateLimitRequest request)
    {
        var rawKey = string.Join(
            '|',
            request.ActorId.Value.ToString("N"),
            Normalize(request.RequestContext.RemoteIpAddress));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(hash);
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? "-"
            : value.Trim().ToLowerInvariant();

    private sealed class RateLimitBucket(DateTimeOffset windowStartedAt)
    {
        public DateTimeOffset WindowStartedAt { get; set; } = windowStartedAt;

        public int AttemptCount { get; set; }

        public DateTimeOffset? BlockedUntil { get; set; }
    }
}
