using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Users;

public readonly record struct EmailAddress
{
    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static EmailAddress Create(string value)
    {
        var email = Guard.AgainstBlank(value, nameof(value), 254).ToLowerInvariant();

        if (!email.Contains('@', StringComparison.Ordinal) || email.StartsWith('@') || email.EndsWith('@'))
        {
            throw new ArgumentException("L'adresse email est invalide.", nameof(value));
        }

        return new EmailAddress(email);
    }

    public override string ToString() => Value;
}
