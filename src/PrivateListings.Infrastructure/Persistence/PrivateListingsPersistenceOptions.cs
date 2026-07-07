namespace PrivateListings.Infrastructure.Persistence;

public sealed class PrivateListingsPersistenceOptions
{
    public const string SectionName = "PrivateListings:Persistence";

    public string ConnectionName { get; set; } = "privatelistingsdb";

    public bool ApplyMigrationsOnStartup { get; set; }
}
