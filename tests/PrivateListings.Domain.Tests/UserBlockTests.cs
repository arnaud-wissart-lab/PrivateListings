using PrivateListings.Domain.Common;
using PrivateListings.Domain.Messaging;

namespace PrivateListings.Domain.Tests;

public sealed class UserBlockTests
{
    [Fact]
    public void CreateBuildsActiveUserBlock()
    {
        var blockerId = UserProfileId.New();
        var blockedId = UserProfileId.New();
        var createdAt = new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero);

        var userBlock = UserBlock.Create(blockerId, blockedId, createdAt);

        Assert.Equal(blockerId, userBlock.BlockerId);
        Assert.Equal(blockedId, userBlock.BlockedId);
        Assert.True(userBlock.IsActive);
    }

    [Fact]
    public void CreateRefusesSelfBlock()
    {
        var userId = UserProfileId.New();
        var createdAt = new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => UserBlock.Create(userId, userId, createdAt));
    }
}
