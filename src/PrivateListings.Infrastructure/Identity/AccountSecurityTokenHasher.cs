using System.Security.Cryptography;
using System.Text;

namespace PrivateListings.Infrastructure.Identity;

internal static class AccountSecurityTokenHasher
{
    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
