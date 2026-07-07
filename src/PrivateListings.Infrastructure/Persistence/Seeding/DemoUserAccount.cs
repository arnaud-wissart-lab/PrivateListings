using PrivateListings.Domain.Common;

namespace PrivateListings.Infrastructure.Persistence.Seeding;

public sealed class DemoUserAccount
{
    private DemoUserAccount(
        Guid id,
        UserProfileId userProfileId,
        string email,
        DemoUserRole role,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserProfileId = userProfileId;
        Email = email;
        Role = role;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    private DemoUserAccount()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public Guid Id { get; private set; }

    public UserProfileId UserProfileId { get; private set; }

    public string Email { get; private set; }

    public DemoUserRole Role { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static DemoUserAccount Create(
        UserProfileId userProfileId,
        string email,
        DemoUserRole role,
        string password,
        DateTimeOffset createdAt)
        => new(
            Guid.NewGuid(),
            userProfileId,
            email.Trim().ToLowerInvariant(),
            role,
            DemoPasswordHasher.Hash(password),
            createdAt);
}
