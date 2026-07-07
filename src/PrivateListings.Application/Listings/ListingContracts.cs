using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Application.Listings;

public sealed record ListingSearchQuery(
    string? Text = null,
    ListingType? Type = null,
    ListingCategoryId? CategoryId = null,
    string? ApproximateLocation = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    DateTimeOffset? EventDate = null,
    int PageSize = 60);

public sealed record ListingCategoryOption(
    ListingCategoryId Id,
    string Name,
    string? Description);

public sealed record ListingImageSummary(
    ListingImageId Id,
    string ContentType,
    string OriginalFileName,
    string? AltText,
    int SortOrder);

public sealed record ListingSummary(
    ListingId Id,
    string Title,
    string Description,
    ListingType Type,
    ListingStatus Status,
    string CategoryName,
    string? ApproximateLocation,
    decimal? Price,
    DateTimeOffset? EventDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ExpiresAt,
    ListingImageSummary? CoverImage);

public sealed record ListingDetails(
    ListingId Id,
    UserProfileId OwnerId,
    string OwnerDisplayName,
    ListingCategoryId CategoryId,
    string CategoryName,
    string Title,
    string Description,
    ListingType Type,
    ListingStatus Status,
    string? ApproximateLocation,
    decimal? Price,
    DateTimeOffset? EventDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? DeletedAt,
    DateTimeOffset? SuspendedAt,
    string? SuspensionReason,
    IReadOnlyList<ListingImageSummary> Images);

public sealed record CreateListingCommand(
    UserProfileId OwnerId,
    ListingCategoryId CategoryId,
    string Title,
    string Description,
    ListingType Type,
    string? ApproximateLocation,
    decimal? Price,
    DateTimeOffset? EventDate,
    DateTimeOffset? ExpiresAt,
    ListingRequestContext RequestContext);

public sealed record UpdateListingCommand(
    UserProfileId ActorId,
    ListingId ListingId,
    ListingCategoryId CategoryId,
    string Title,
    string Description,
    ListingType Type,
    string? ApproximateLocation,
    decimal? Price,
    DateTimeOffset? EventDate,
    DateTimeOffset? ExpiresAt);

public sealed record UploadListingImageCommand(
    UserProfileId ActorId,
    ListingId ListingId,
    string OriginalFileName,
    string ContentType,
    Stream Content,
    long SizeInBytes,
    string? AltText,
    ListingRequestContext RequestContext);

public sealed record SuspendListingCommand(
    UserProfileId ActorId,
    ListingId ListingId,
    string Reason,
    bool CanModerate);

public sealed record ListingRequestContext(string? RemoteIpAddress, string? UserAgent);

public sealed record ListingImageFile(
    Stream Content,
    string ContentType,
    string FileName,
    DateTimeOffset UploadedAt);

public enum ListingOperationStatus
{
    Success,
    NotFound,
    Forbidden,
    ValidationFailed,
    RateLimited
}

public sealed record ListingOperationResult(
    ListingOperationStatus Status,
    ListingId? ListingId = null,
    IReadOnlyList<string>? Errors = null,
    TimeSpan? RetryAfter = null)
{
    public bool Succeeded => Status == ListingOperationStatus.Success;

    public static ListingOperationResult Success(ListingId? listingId = null)
        => new(ListingOperationStatus.Success, listingId);

    public static ListingOperationResult NotFound()
        => new(ListingOperationStatus.NotFound, Errors: ["Annonce introuvable."]);

    public static ListingOperationResult Forbidden()
        => new(ListingOperationStatus.Forbidden, Errors: ["Vous n'êtes pas autorisé à modifier cette annonce."]);

    public static ListingOperationResult Forbidden(string error)
        => new(ListingOperationStatus.Forbidden, Errors: [error]);

    public static ListingOperationResult ValidationFailed(params string[] errors)
        => new(ListingOperationStatus.ValidationFailed, Errors: errors);

    public static ListingOperationResult RateLimited(TimeSpan? retryAfter)
        => new(ListingOperationStatus.RateLimited, Errors: ["Trop de tentatives. Réessayez plus tard."], RetryAfter: retryAfter);
}
