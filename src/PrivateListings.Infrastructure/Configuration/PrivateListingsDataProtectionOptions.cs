namespace PrivateListings.Infrastructure.Configuration;

public sealed class PrivateListingsDataProtectionOptions
{
    public const string SectionName = "PrivateListings:DataProtection";

    public string? KeysPath { get; set; }
}
