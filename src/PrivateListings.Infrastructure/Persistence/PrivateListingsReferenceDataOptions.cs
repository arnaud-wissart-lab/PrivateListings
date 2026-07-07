namespace PrivateListings.Infrastructure.Persistence;

public sealed class PrivateListingsReferenceDataOptions
{
    public const string SectionName = "PrivateListings:ReferenceData";

    public bool SeedInitialCategories { get; set; }
}
