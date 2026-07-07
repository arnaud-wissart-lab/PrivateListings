using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace PrivateListings.Application.Authentication;

public sealed class InMemoryAccountRateLimiter(
    IOptions<AccountRateLimitOptions> options,
    TimeProvider timeProvider)
    : IAccountRateLimiter
{
    private readonly ConcurrentDictionary<string, RateLimitBucket> buckets = new();

    public ValueTask<AccountRateLimitLease> TryAcquireAsync(
        AccountRateLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = timeProvider.GetUtcNow();
        var policy = options.Value;
        var key = BuildKey(request);
        var bucket = buckets.GetOrAdd(key, _ => new RateLimitBucket(now));

        lock (bucket)
        {
            if (bucket.BlockedUntil is { } blockedUntil && blockedUntil > now)
            {
                return ValueTask.FromResult(AccountRateLimitLease.Denied(blockedUntil - now));
            }

            if (now - bucket.WindowStartedAt >= policy.Window)
            {
                bucket.WindowStartedAt = now;
                bucket.AttemptCount = 0;
                bucket.BlockedUntil = null;
            }

            bucket.AttemptCount++;

            if (bucket.AttemptCount <= policy.GetPermitLimit(request.Action))
            {
                return ValueTask.FromResult(AccountRateLimitLease.Allowed);
            }

            bucket.BlockedUntil = now.Add(policy.BlockDuration);
            return ValueTask.FromResult(AccountRateLimitLease.Denied(policy.BlockDuration));
        }
    }

    public ValueTask ResetAsync(
        AccountRateLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        buckets.TryRemove(BuildKey(request), out _);

        return ValueTask.CompletedTask;
    }

    private static string BuildKey(AccountRateLimitRequest request)
    {
        var identifier = Normalize(request.Identifier);
        var remoteIpAddress = Normalize(request.RemoteIpAddress);
        var rawKey = $"{request.Action}|{identifier}|{remoteIpAddress}";
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
