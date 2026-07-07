namespace PrivateListings.Domain.Common;

internal static class Guard
{
    public static void AgainstEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("L'identifiant ne peut pas être vide.", parameterName);
        }
    }

    public static string AgainstBlank(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("La valeur ne peut pas être vide.", parameterName);
        }

        var trimmedValue = value.Trim();
        if (trimmedValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"La valeur ne peut pas dépasser {maxLength} caractères.");
        }

        return trimmedValue;
    }

    public static string? OptionalText(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim();
        if (trimmedValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"La valeur ne peut pas dépasser {maxLength} caractères.");
        }

        return trimmedValue;
    }

    public static DateTimeOffset AgainstDefault(DateTimeOffset value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException("La date ne peut pas être la valeur par défaut.", parameterName);
        }

        return value;
    }
}
