namespace PrivateListings.Application.Authentication;

public sealed class AccountPasswordPolicyOptions
{
    public const int DefaultRequiredLength = 12;
    public const int DefaultMaximumLength = 256;

    public int RequiredLength { get; set; } = DefaultRequiredLength;

    public int MaximumLength { get; set; } = DefaultMaximumLength;
}
