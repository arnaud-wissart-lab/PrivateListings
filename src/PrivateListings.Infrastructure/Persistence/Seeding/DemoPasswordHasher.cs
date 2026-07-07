using System.Security.Cryptography;

namespace PrivateListings.Infrastructure.Persistence.Seeding;

internal static class DemoPasswordHasher
{
    private const int SaltLength = 16;
    private const int HashLength = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashLength);

        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
