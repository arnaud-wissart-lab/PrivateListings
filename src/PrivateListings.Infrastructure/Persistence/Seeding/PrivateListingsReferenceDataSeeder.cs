using Microsoft.EntityFrameworkCore;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Infrastructure.Persistence.Seeding;

public sealed class PrivateListingsReferenceDataSeeder(PrivateListingsDbContext dbContext)
{
    public static readonly IReadOnlyList<string> InitialCategoryNames =
    [
        "Vente",
        "Don",
        "Recherche",
        "Services",
        "Événements",
        "Emploi / missions",
        "Projets / entraide"
    ];

    private static readonly Dictionary<string, string> CategoryDescriptions = new()
    {
        ["Vente"] = "Objets proposés à la vente entre membres.",
        ["Don"] = "Objets proposés gratuitement.",
        ["Recherche"] = "Demandes d'objets, d'informations ou de coups de main.",
        ["Services"] = "Petits services et entraide pratique.",
        ["Événements"] = "Rencontres, ateliers et moments conviviaux fictifs.",
        ["Emploi / missions"] = "Missions courtes et besoins ponctuels.",
        ["Projets / entraide"] = "Projets locaux et entraide collective."
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingNames = await dbContext.ListingCategories
            .Select(category => category.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var missingNames = InitialCategoryNames
            .Except(existingNames, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingNames.Count == 0)
        {
            return;
        }

        var createdAt = new DateTimeOffset(2026, 7, 7, 8, 0, 0, TimeSpan.Zero);
        var categories = missingNames.Select(name =>
            ListingCategory.Create(name, createdAt, CategoryDescriptions[name]));

        await dbContext.ListingCategories.AddRangeAsync(categories, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
