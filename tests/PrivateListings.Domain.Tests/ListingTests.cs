using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Domain.Tests;

public sealed class ListingTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 7, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateBuildsValidDraftListing()
    {
        var ownerId = UserProfileId.New();
        var categoryId = ListingCategoryId.New();
        var expiresAt = CreatedAt.AddDays(30);

        var listing = Listing.Create(
            ownerId,
            categoryId,
            "Vélo enfant 20 pouces",
            "Vélo en bon état, disponible dans le quartier.",
            ListingType.Sell,
            CreatedAt,
            expiresAt);

        Assert.NotEqual(default, listing.Id);
        Assert.Equal(ownerId, listing.OwnerId);
        Assert.Equal(categoryId, listing.CategoryId);
        Assert.Equal("Vélo enfant 20 pouces", listing.Title);
        Assert.Equal(ListingType.Sell, listing.Type);
        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal(expiresAt, listing.ExpiresAt);
    }

    [Fact]
    public void PublishMovesDraftListingToPublishedStatus()
    {
        var publishedAt = CreatedAt.AddHours(1);
        var listing = CreateListing(expiresAt: CreatedAt.AddDays(7));

        listing.Publish(publishedAt);

        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.Equal(publishedAt, listing.PublishedAt);
        Assert.Equal(publishedAt, listing.UpdatedAt);
    }

    [Fact]
    public void PublishWithPastExpirationMarksListingAsExpired()
    {
        var publishedAt = CreatedAt.AddHours(1);
        var listing = CreateListing(expiresAt: CreatedAt.AddMinutes(30));

        listing.Publish(publishedAt);

        Assert.Equal(ListingStatus.Expired, listing.Status);
        Assert.Equal(ListingStatus.Expired, listing.GetStatusAt(publishedAt));
    }

    [Fact]
    public void ExpireIfNeededMovesPublishedListingToExpiredStatus()
    {
        var publishedAt = CreatedAt.AddHours(1);
        var expiresAt = CreatedAt.AddDays(1);
        var listing = CreateListing(expiresAt);
        listing.Publish(publishedAt);

        var expired = listing.ExpireIfNeeded(expiresAt.AddSeconds(1));

        Assert.True(expired);
        Assert.Equal(ListingStatus.Expired, listing.Status);
    }

    [Fact]
    public void DeleteMarksListingAsDeletedWithoutRemovingItsIdentity()
    {
        var deletedAt = CreatedAt.AddHours(2);
        var listing = CreateListing(expiresAt: null);
        var listingId = listing.Id;

        listing.Delete(deletedAt);

        Assert.Equal(listingId, listing.Id);
        Assert.Equal(ListingStatus.Deleted, listing.Status);
        Assert.Equal(deletedAt, listing.DeletedAt);
    }

    private static Listing CreateListing(DateTimeOffset? expiresAt)
        => Listing.Create(
            UserProfileId.New(),
            ListingCategoryId.New(),
            "Tondeuse à prêter",
            "Disponible le week-end pour les voisins proches.",
            ListingType.Give,
            CreatedAt,
            expiresAt);
}
