using Microsoft.EntityFrameworkCore;
using PrivateListings.Infrastructure.Persistence;

namespace PrivateListings.Infrastructure.Identity;

public sealed class AccountSecurityTokenStore(
    PrivateListingsDbContext dbContext,
    TimeProvider timeProvider)
{
    public const string EmailConfirmationPurpose = "EmailConfirmation";
    public const string PasswordResetPurpose = "PasswordReset";

    public async Task StoreAsync(
        Guid userId,
        string purpose,
        string token,
        TimeSpan lifespan,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var activeTokens = await dbContext.AccountSecurityTokens
            .Where(storedToken =>
                storedToken.UserId == userId
                && storedToken.Purpose == purpose
                && storedToken.ConsumedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var activeToken in activeTokens)
        {
            activeToken.Consume(now);
        }

        dbContext.AccountSecurityTokens.Add(AccountSecurityToken.Create(
            userId,
            purpose,
            token,
            now,
            now.Add(lifespan)));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TryConsumeAsync(
        Guid userId,
        string purpose,
        string token,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var tokenHash = AccountSecurityTokenHasher.Hash(token);
        var storedToken = await dbContext.AccountSecurityTokens
            .SingleOrDefaultAsync(candidate =>
                candidate.UserId == userId
                && candidate.Purpose == purpose
                && candidate.TokenHash == tokenHash,
                cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null || storedToken.IsConsumed || storedToken.IsExpired(now))
        {
            return false;
        }

        storedToken.Consume(now);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }
}
