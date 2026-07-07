namespace PrivateListings.Application.Authentication;

public sealed record AccountPasswordValidationResult(bool Succeeded, IReadOnlyCollection<string> Errors)
{
    public static AccountPasswordValidationResult Success { get; } = new(true, []);

    public static AccountPasswordValidationResult Failed(params string[] errors)
        => new(false, errors);
}
