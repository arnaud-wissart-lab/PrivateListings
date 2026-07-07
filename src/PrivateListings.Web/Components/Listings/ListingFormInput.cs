using System.ComponentModel.DataAnnotations;
using PrivateListings.Application.Listings;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Web.Components.Listings;

public sealed class ListingFormInput
{
    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(160, ErrorMessage = "Le titre est trop long.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "La description est obligatoire.")]
    [StringLength(4_000, MinimumLength = 10, ErrorMessage = "La description doit contenir entre 10 et 4 000 caractères.")]
    public string Description { get; set; } = string.Empty;

    public ListingType Type { get; set; } = ListingType.Sell;

    [Required(ErrorMessage = "La catégorie est obligatoire.")]
    public string CategoryId { get; set; } = string.Empty;

    [Required(ErrorMessage = "La localisation approximative est obligatoire.")]
    [StringLength(120, ErrorMessage = "La localisation approximative est trop longue.")]
    public string ApproximateLocation { get; set; } = string.Empty;

    [Range(0, 999_999_999, ErrorMessage = "Le prix ne peut pas être négatif.")]
    public decimal? Price { get; set; }

    public DateTime? EventDate { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public static ListingFormInput FromDetails(ListingDetails details)
        => new()
        {
            Title = details.Title,
            Description = details.Description,
            Type = details.Type,
            CategoryId = details.CategoryId.Value.ToString("D"),
            ApproximateLocation = details.ApproximateLocation ?? string.Empty,
            Price = details.Price,
            EventDate = details.EventDate?.Date,
            ExpiresAt = details.ExpiresAt?.AddDays(-1).Date
        };

    public bool TryBuildCategoryId(out ListingCategoryId categoryId)
    {
        if (Guid.TryParse(CategoryId, out var value))
        {
            categoryId = ListingCategoryId.From(value);
            return true;
        }

        categoryId = default;
        return false;
    }

    public DateTimeOffset? BuildEventDate()
        => EventDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(EventDate.Value.Date, DateTimeKind.Unspecified), TimeSpan.Zero)
            : null;

    public DateTimeOffset? BuildExpirationDate()
        => ExpiresAt.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(ExpiresAt.Value.Date.AddDays(1), DateTimeKind.Unspecified), TimeSpan.Zero)
            : null;
}
