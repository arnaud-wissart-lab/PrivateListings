using PrivateListings.Domain.Listings;

namespace PrivateListings.Web.Components.Listings;

internal static class ListingDisplay
{
    public static string TypeLabel(ListingType type)
        => type switch
        {
            ListingType.Sell => "Je vends",
            ListingType.Give => "Je donne",
            ListingType.Search => "Je cherche",
            ListingType.Service => "Service",
            ListingType.Event => "Événement",
            ListingType.Job => "Emploi / mission",
            ListingType.Project => "Projet / entraide",
            _ => type.ToString()
        };

    public static string StatusLabel(ListingStatus status)
        => status switch
        {
            ListingStatus.Draft => "Brouillon",
            ListingStatus.Published => "Publiée",
            ListingStatus.Expired => "Expirée",
            ListingStatus.Deleted => "Supprimée",
            ListingStatus.Suspended => "Suspendue",
            _ => status.ToString()
        };

    public static string StatusClass(ListingStatus status)
        => status switch
        {
            ListingStatus.Published => "is-published",
            ListingStatus.Draft => "is-draft",
            ListingStatus.Expired => "is-expired",
            ListingStatus.Suspended => "is-suspended",
            ListingStatus.Deleted => "is-deleted",
            _ => "is-draft"
        };

    public static string FormatPrice(decimal? price)
        => price.HasValue ? $"{price.Value:0.##} €" : "Prix non renseigné";

    public static string ImageUrl(Guid imageId)
        => $"/annonces/images/{imageId:D}";
}
