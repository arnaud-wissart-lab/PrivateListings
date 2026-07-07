using Microsoft.EntityFrameworkCore;
using PrivateListings.Application.Listings;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Infrastructure.Persistence;

public sealed class EfListingRepository(PrivateListingsDbContext dbContext) : IListingRepository
{
    public async Task<IReadOnlyList<ListingCategoryOption>> ListActiveCategoriesAsync(CancellationToken cancellationToken)
        => await dbContext.ListingCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new ListingCategoryOption(category.Id, category.Name, category.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<bool> ActiveCategoryExistsAsync(
        ListingCategoryId categoryId,
        CancellationToken cancellationToken)
        => dbContext.ListingCategories
            .AsNoTracking()
            .AnyAsync(category => category.Id == categoryId && category.IsActive, cancellationToken);

    public Task<bool> IsOwnerSuspendedAsync(
        UserProfileId ownerId,
        CancellationToken cancellationToken)
        => dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.UserProfileId == ownerId && user.SuspendedAt.HasValue, cancellationToken);

    public async Task<IReadOnlyList<ListingSummary>> SearchPublishedAsync(
        ListingSearchQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var listings = ApplySearchFilters(
            dbContext.Listings
                .AsNoTracking()
                .Where(listing => listing.Status == ListingStatus.Published)
                .Where(listing => !listing.ExpiresAt.HasValue || listing.ExpiresAt.Value > now),
            query);

        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var orderedListings = listings
            .OrderByDescending(listing => listing.PublishedAt ?? listing.CreatedAt)
            .Take(pageSize);

        return await BuildSummaryQuery(orderedListings)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ListingSummary>> ListOwnerListingsAsync(
        UserProfileId ownerId,
        CancellationToken cancellationToken)
    {
        var listings = dbContext.Listings
            .AsNoTracking()
            .Where(listing => listing.OwnerId == ownerId && listing.Status != ListingStatus.Deleted);

        return await BuildSummaryQuery(listings.OrderByDescending(listing => listing.UpdatedAt ?? listing.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<ListingDetails?> GetPublicDetailsAsync(
        ListingId listingId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var listings = dbContext.Listings
            .AsNoTracking()
            .Where(listing => listing.Id == listingId)
            .Where(listing => listing.Status == ListingStatus.Published)
            .Where(listing => !listing.ExpiresAt.HasValue || listing.ExpiresAt.Value > now);

        return BuildDetailsQuery(listings).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<ListingDetails?> GetDetailsAsync(
        ListingId listingId,
        CancellationToken cancellationToken)
    {
        var listings = dbContext.Listings
            .AsNoTracking()
            .Where(listing => listing.Id == listingId);

        return BuildDetailsQuery(listings).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Listing?> GetForUpdateAsync(
        ListingId listingId,
        CancellationToken cancellationToken)
        => dbContext.Listings
            .Include(listing => listing.Images)
            .SingleOrDefaultAsync(listing => listing.Id == listingId, cancellationToken);

    public Task<ListingImageAccess?> GetImageAccessAsync(
        ListingImageId imageId,
        CancellationToken cancellationToken)
        => (from image in dbContext.ListingImages.AsNoTracking()
            join listing in dbContext.Listings.AsNoTracking() on image.ListingId equals listing.Id
            where image.Id == imageId
            select new ListingImageAccess(
                image.Id,
                listing.Id,
                listing.OwnerId,
                listing.Status,
                listing.ExpiresAt,
                image.StorageKey,
                image.ContentType,
                image.OriginalFileName,
                image.UploadedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Listing listing, CancellationToken cancellationToken)
        => await dbContext.Listings.AddAsync(listing, cancellationToken).ConfigureAwait(false);

    public async Task<int> ExpirePublishedAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var listings = await dbContext.Listings
            .Where(listing => listing.Status == ListingStatus.Published)
            .Where(listing => listing.ExpiresAt.HasValue && listing.ExpiresAt.Value <= now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var listing in listings)
        {
            listing.ExpireIfNeeded(now);
        }

        if (listings.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return listings.Count;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<ListingSummary> BuildSummaryQuery(IQueryable<Listing> listings)
        => from listing in listings
           join category in dbContext.ListingCategories.AsNoTracking() on listing.CategoryId equals category.Id
           select new ListingSummary(
               listing.Id,
               listing.Title,
               listing.Description,
               listing.Type,
               listing.Status,
               category.Name,
               listing.ApproximateLocation,
               listing.Price,
               listing.EventDate,
               listing.CreatedAt,
               listing.PublishedAt,
               listing.ExpiresAt,
               listing.Images
                   .OrderBy(image => image.SortOrder)
                   .Select(image => new ListingImageSummary(
                       image.Id,
                       image.ContentType,
                       image.OriginalFileName,
                       image.AltText,
                       image.SortOrder))
                   .FirstOrDefault());

    private IQueryable<ListingDetails> BuildDetailsQuery(IQueryable<Listing> listings)
        => from listing in listings
           join category in dbContext.ListingCategories.AsNoTracking() on listing.CategoryId equals category.Id
           join owner in dbContext.UserProfiles.AsNoTracking() on listing.OwnerId equals owner.Id
           select new ListingDetails(
               listing.Id,
               listing.OwnerId,
               owner.DisplayName,
               listing.CategoryId,
               category.Name,
               listing.Title,
               listing.Description,
               listing.Type,
               listing.Status,
               listing.ApproximateLocation,
               listing.Price,
               listing.EventDate,
               listing.CreatedAt,
               listing.UpdatedAt,
               listing.PublishedAt,
               listing.ExpiresAt,
               listing.DeletedAt,
               listing.SuspendedAt,
               listing.SuspensionReason,
               listing.Images
                   .OrderBy(image => image.SortOrder)
                   .Select(image => new ListingImageSummary(
                       image.Id,
                       image.ContentType,
                       image.OriginalFileName,
                       image.AltText,
                       image.SortOrder))
                   .ToList());

    private static IQueryable<Listing> ApplySearchFilters(
        IQueryable<Listing> listings,
        ListingSearchQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var pattern = BuildContainsPattern(query.Text);
            listings = listings.Where(listing =>
                EF.Functions.ILike(listing.Title, pattern)
                || EF.Functions.ILike(listing.Description, pattern)
                || (listing.ApproximateLocation != null && EF.Functions.ILike(listing.ApproximateLocation, pattern)));
        }

        if (query.Type.HasValue)
        {
            listings = listings.Where(listing => listing.Type == query.Type.Value);
        }

        if (query.CategoryId.HasValue)
        {
            listings = listings.Where(listing => listing.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ApproximateLocation))
        {
            var locationPattern = BuildContainsPattern(query.ApproximateLocation);
            listings = listings.Where(listing =>
                listing.ApproximateLocation != null && EF.Functions.ILike(listing.ApproximateLocation, locationPattern));
        }

        if (query.MinPrice.HasValue)
        {
            listings = listings.Where(listing => listing.Price.HasValue && listing.Price.Value >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            listings = listings.Where(listing => listing.Price.HasValue && listing.Price.Value <= query.MaxPrice.Value);
        }

        if (query.EventDate.HasValue)
        {
            var start = new DateTimeOffset(query.EventDate.Value.Date, TimeSpan.Zero);
            var end = start.AddDays(1);
            listings = listings.Where(listing =>
                listing.EventDate.HasValue
                && listing.EventDate.Value >= start
                && listing.EventDate.Value < end);
        }

        return listings;
    }

    private static string BuildContainsPattern(string value)
        => $"%{value.Trim()}%";
}
