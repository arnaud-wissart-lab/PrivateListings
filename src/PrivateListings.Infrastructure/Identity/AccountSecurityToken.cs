namespace PrivateListings.Infrastructure.Identity;

public sealed class AccountSecurityToken
{
    private AccountSecurityToken()
    {
        Purpose = string.Empty;
        TokenHash = string.Empty;
    }

    private AccountSecurityToken(
        Guid id,
        Guid userId,
        string purpose,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Purpose { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool IsConsumed => ConsumedAt.HasValue;

    public static AccountSecurityToken Create(
        Guid userId,
        string purpose,
        string token,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
        => new(
            Guid.NewGuid(),
            userId,
            purpose,
            AccountSecurityTokenHasher.Hash(token),
            createdAt,
            expiresAt);

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    public void Consume(DateTimeOffset consumedAt)
    {
        if (IsConsumed)
        {
            return;
        }

        ConsumedAt = consumedAt;
    }
}
