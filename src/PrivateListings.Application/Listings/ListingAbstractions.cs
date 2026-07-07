using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Application.Listings;

public interface IListingService
{
    Task<IReadOnlyList<ListingCategoryOption>> ListCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListingSummary>> SearchPublishedAsync(
        ListingSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListingSummary>> ListOwnerListingsAsync(
        UserProfileId ownerId,
        CancellationToken cancellationToken = default);

    Task<ListingDetails?> GetPublicDetailsAsync(
        ListingId listingId,
        CancellationToken cancellationToken = default);

    Task<ListingDetails?> GetOwnerDetailsAsync(
        ListingId listingId,
        UserProfileId ownerId,
        bool canModerate,
        CancellationToken cancellationToken = default);

    Task<ListingOperationResult> CreateAsync(
        CreateListingCommand command,
        CancellationToken cancellationToken = default);

    Task<ListingOperationResult> UpdateAsync(
        UpdateListingCommand command,
        CancellationToken cancellationToken = default);

    Task<ListingOperationResult> PublishAsync(
        ListingId listingId,
        UserProfileId actorId,
        CancellationToken cancellationToken = default);

    Task<ListingOperationResult> MoveToDraftAsync(
        ListingId listingId,
        UserProfileId actorId,
        CancellationToken cancellationToken = default);

    Task<ListingOperationResult> DeleteAsync(
        ListingId listingId,
        UserProfileId actorId,
        CancellationToken cancellationToken = default);

    Task<ListingOperationResult> SuspendAsync(
        SuspendListingCommand command,
        CancellationToken cancellationToken = default);

    Task<ListingOperationResult> UploadImageAsync(
        UploadListingImageCommand command,
        CancellationToken cancellationToken = default);

    Task<ListingImageFile?> OpenImageAsync(
        ListingImageId imageId,
        UserProfileId? actorId,
        bool canModerate,
        CancellationToken cancellationToken = default);

    Task<int> ExpirePublishedAsync(CancellationToken cancellationToken = default);
}

public interface IListingRepository
{
    Task<IReadOnlyList<ListingCategoryOption>> ListActiveCategoriesAsync(CancellationToken cancellationToken);

    Task<bool> ActiveCategoryExistsAsync(
        ListingCategoryId categoryId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ListingSummary>> SearchPublishedAsync(
        ListingSearchQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ListingSummary>> ListOwnerListingsAsync(
        UserProfileId ownerId,
        CancellationToken cancellationToken);

    Task<ListingDetails?> GetPublicDetailsAsync(
        ListingId listingId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<ListingDetails?> GetDetailsAsync(
        ListingId listingId,
        CancellationToken cancellationToken);

    Task<Listing?> GetForUpdateAsync(
        ListingId listingId,
        CancellationToken cancellationToken);

    Task<ListingImageAccess?> GetImageAccessAsync(
        ListingImageId imageId,
        CancellationToken cancellationToken);

    Task AddAsync(Listing listing, CancellationToken cancellationToken);

    Task<int> ExpirePublishedAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IListingImageStorage
{
    Task<StoredListingImage> SaveAsync(
        ListingImageUpload upload,
        CancellationToken cancellationToken = default);

    Task<ListingImageFile?> OpenAsync(
        ListingImageAccess image,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public interface IListingRateLimiter
{
    Task<ListingRateLimitLease> TryAcquireAsync(
        ListingRateLimitRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ListingImageUpload(
    string OriginalFileName,
    string ContentType,
    Stream Content,
    long SizeInBytes);

public sealed record StoredListingImage(
    string StorageKey,
    string ContentType,
    long SizeInBytes);

public sealed record ListingImageAccess(
    ListingImageId ImageId,
    ListingId ListingId,
    UserProfileId OwnerId,
    ListingStatus ListingStatus,
    DateTimeOffset? ExpiresAt,
    string StorageKey,
    string ContentType,
    string OriginalFileName,
    DateTimeOffset UploadedAt);

public sealed record ListingRateLimitRequest(
    ListingRateLimitAction Action,
    UserProfileId ActorId,
    ListingRequestContext RequestContext);

public sealed record ListingRateLimitLease(bool IsAllowed, TimeSpan? RetryAfter = null);

public enum ListingRateLimitAction
{
    Create,
    UploadImage
}
