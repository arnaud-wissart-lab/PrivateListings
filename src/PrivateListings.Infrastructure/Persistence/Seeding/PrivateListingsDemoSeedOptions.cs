namespace PrivateListings.Infrastructure.Persistence.Seeding;

public sealed class PrivateListingsDemoSeedOptions
{
    public const string SectionName = "PrivateListings:DemoSeed";

    public bool Enabled { get; set; }

    public PrivateListingsDemoSeedAccountsOptions Accounts { get; set; } = new();
}

public sealed class PrivateListingsDemoSeedAccountsOptions
{
    public PrivateListingsDemoSeedAccountOptions Admin { get; set; } = new()
    {
        Email = "admin-demo@example.test",
        DisplayName = "Administrateur Démo"
    };

    public PrivateListingsDemoSeedAccountOptions Moderator { get; set; } = new()
    {
        Email = "moderateur-demo@example.test",
        DisplayName = "Modérateur Démo"
    };

    public PrivateListingsDemoSeedAccountOptions Member { get; set; } = new()
    {
        Email = "membre-demo@example.test",
        DisplayName = "Membre Démo"
    };
}

public sealed class PrivateListingsDemoSeedAccountOptions
{
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
