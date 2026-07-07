using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Messaging;

public sealed class UserBlock
{
    private UserBlock()
    {
    }

    private UserBlock(
        UserProfileId blockerId,
        UserProfileId blockedId,
        DateTimeOffset createdAt)
    {
        Guard.AgainstEmpty(blockerId.Value, nameof(blockerId));
        Guard.AgainstEmpty(blockedId.Value, nameof(blockedId));

        if (blockerId == blockedId)
        {
            throw new ArgumentException("Un utilisateur ne peut pas se bloquer lui-même.", nameof(blockedId));
        }

        BlockerId = blockerId;
        BlockedId = blockedId;
        CreatedAt = Guard.AgainstDefault(createdAt, nameof(createdAt));
    }

    public UserProfileId BlockerId { get; }

    public UserProfileId BlockedId { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => !RevokedAt.HasValue;

    public static UserBlock Create(
        UserProfileId blockerId,
        UserProfileId blockedId,
        DateTimeOffset createdAt)
        => new(blockerId, blockedId, createdAt);

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (!IsActive)
        {
            return;
        }

        RevokedAt = Guard.AgainstDefault(revokedAt, nameof(revokedAt));
    }
}
