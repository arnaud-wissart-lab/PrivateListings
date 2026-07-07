using Microsoft.Extensions.Options;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Application.Listings;

public sealed class ListingService(
    IListingRepository repository,
    IListingImageStorage imageStorage,
    IListingRateLimiter rateLimiter,
    IOptions<ListingImageOptions> imageOptions,
    TimeProvider timeProvider)
    : IListingService
{
    public Task<IReadOnlyList<ListingCategoryOption>> ListCategoriesAsync(CancellationToken cancellationToken = default)
        => repository.ListActiveCategoriesAsync(cancellationToken);

    public Task<IReadOnlyList<ListingSummary>> SearchPublishedAsync(
        ListingSearchQuery query,
        CancellationToken cancellationToken = default)
        => repository.SearchPublishedAsync(query, timeProvider.GetUtcNow(), cancellationToken);

    public Task<IReadOnlyList<ListingSummary>> ListOwnerListingsAsync(
        UserProfileId ownerId,
        CancellationToken cancellationToken = default)
        => repository.ListOwnerListingsAsync(ownerId, cancellationToken);

    public Task<ListingDetails?> GetPublicDetailsAsync(
        ListingId listingId,
        CancellationToken cancellationToken = default)
        => repository.GetPublicDetailsAsync(listingId, timeProvider.GetUtcNow(), cancellationToken);

    public async Task<ListingDetails?> GetOwnerDetailsAsync(
        ListingId listingId,
        UserProfileId ownerId,
        bool canModerate,
        CancellationToken cancellationToken = default)
    {
        var details = await repository.GetDetailsAsync(listingId, cancellationToken).ConfigureAwait(false);
        if (details is null)
        {
            return null;
        }

        return details.OwnerId == ownerId || canModerate ? details : null;
    }

    public async Task<ListingOperationResult> CreateAsync(
        CreateListingCommand command,
        CancellationToken cancellationToken = default)
    {
        var rateLimit = await CheckRateLimitAsync(
            ListingRateLimitAction.Create,
            command.OwnerId,
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);

        if (rateLimit is not null)
        {
            return rateLimit;
        }

        if (!await repository.ActiveCategoryExistsAsync(command.CategoryId, cancellationToken).ConfigureAwait(false))
        {
            return ListingOperationResult.ValidationFailed("La catégorie sélectionnée est invalide.");
        }

        try
        {
            var listing = Listing.Create(
                command.OwnerId,
                command.CategoryId,
                command.Title,
                command.Description,
                command.Type,
                timeProvider.GetUtcNow(),
                command.ExpiresAt,
                command.ApproximateLocation,
                command.Price,
                command.EventDate);

            await repository.AddAsync(listing, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ListingOperationResult.Success(listing.Id);
        }
        catch (ArgumentException exception)
        {
            return ListingOperationResult.ValidationFailed(exception.Message);
        }
    }

    public async Task<ListingOperationResult> UpdateAsync(
        UpdateListingCommand command,
        CancellationToken cancellationToken = default)
    {
        var listing = await repository.GetForUpdateAsync(command.ListingId, cancellationToken).ConfigureAwait(false);
        if (listing is null)
        {
            return ListingOperationResult.NotFound();
        }

        if (listing.OwnerId != command.ActorId)
        {
            return ListingOperationResult.Forbidden();
        }

        if (!await repository.ActiveCategoryExistsAsync(command.CategoryId, cancellationToken).ConfigureAwait(false))
        {
            return ListingOperationResult.ValidationFailed("La catégorie sélectionnée est invalide.");
        }

        try
        {
            listing.UpdateDetails(
                command.CategoryId,
                command.Title,
                command.Description,
                command.Type,
                timeProvider.GetUtcNow(),
                command.ExpiresAt,
                command.ApproximateLocation,
                command.Price,
                command.EventDate);

            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ListingOperationResult.Success(listing.Id);
        }
        catch (ArgumentException exception)
        {
            return ListingOperationResult.ValidationFailed(exception.Message);
        }
        catch (DomainException exception)
        {
            return ListingOperationResult.ValidationFailed(exception.Message);
        }
    }

    public async Task<ListingOperationResult> PublishAsync(
        ListingId listingId,
        UserProfileId actorId,
        CancellationToken cancellationToken = default)
    {
        var listing = await repository.GetForUpdateAsync(listingId, cancellationToken).ConfigureAwait(false);
        if (listing is null)
        {
            return ListingOperationResult.NotFound();
        }

        if (listing.OwnerId != actorId)
        {
            return ListingOperationResult.Forbidden();
        }

        try
        {
            listing.Publish(timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ListingOperationResult.Success(listing.Id);
        }
        catch (DomainException exception)
        {
            return ListingOperationResult.ValidationFailed(exception.Message);
        }
    }

    public async Task<ListingOperationResult> MoveToDraftAsync(
        ListingId listingId,
        UserProfileId actorId,
        CancellationToken cancellationToken = default)
    {
        var listing = await repository.GetForUpdateAsync(listingId, cancellationToken).ConfigureAwait(false);
        if (listing is null)
        {
            return ListingOperationResult.NotFound();
        }

        if (listing.OwnerId != actorId)
        {
            return ListingOperationResult.Forbidden();
        }

        try
        {
            listing.MoveToDraft(timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ListingOperationResult.Success(listing.Id);
        }
        catch (DomainException exception)
        {
            return ListingOperationResult.ValidationFailed(exception.Message);
        }
    }

    public async Task<ListingOperationResult> DeleteAsync(
        ListingId listingId,
        UserProfileId actorId,
        CancellationToken cancellationToken = default)
    {
        var listing = await repository.GetForUpdateAsync(listingId, cancellationToken).ConfigureAwait(false);
        if (listing is null)
        {
            return ListingOperationResult.NotFound();
        }

        if (listing.OwnerId != actorId)
        {
            return ListingOperationResult.Forbidden();
        }

        listing.Delete(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ListingOperationResult.Success(listing.Id);
    }

    public async Task<ListingOperationResult> SuspendAsync(
        SuspendListingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.CanModerate)
        {
            return ListingOperationResult.Forbidden();
        }

        var listing = await repository.GetForUpdateAsync(command.ListingId, cancellationToken).ConfigureAwait(false);
        if (listing is null)
        {
            return ListingOperationResult.NotFound();
        }

        try
        {
            listing.Suspend(command.Reason, timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ListingOperationResult.Success(listing.Id);
        }
        catch (ArgumentException exception)
        {
            return ListingOperationResult.ValidationFailed(exception.Message);
        }
        catch (DomainException exception)
        {
            return ListingOperationResult.ValidationFailed(exception.Message);
        }
    }

    public async Task<ListingOperationResult> UploadImageAsync(
        UploadListingImageCommand command,
        CancellationToken cancellationToken = default)
    {
        var rateLimit = await CheckRateLimitAsync(
            ListingRateLimitAction.UploadImage,
            command.ActorId,
            command.RequestContext,
            cancellationToken).ConfigureAwait(false);

        if (rateLimit is not null)
        {
            return rateLimit;
        }

        var listing = await repository.GetForUpdateAsync(command.ListingId, cancellationToken).ConfigureAwait(false);
        if (listing is null)
        {
            return ListingOperationResult.NotFound();
        }

        if (listing.OwnerId != command.ActorId)
        {
            return ListingOperationResult.Forbidden();
        }

        if (listing.Images.Count >= imageOptions.Value.MaxImagesPerListing)
        {
            return ListingOperationResult.ValidationFailed("Le nombre maximal d'images est atteint pour cette annonce.");
        }

        StoredListingImage? storedImage = null;

        try
        {
            storedImage = await imageStorage.SaveAsync(
                new ListingImageUpload(
                    command.OriginalFileName,
                    command.ContentType,
                    command.Content,
                    command.SizeInBytes),
                cancellationToken).ConfigureAwait(false);

            var sortOrder = listing.Images.Count == 0
                ? 0
                : listing.Images.Max(image => image.SortOrder) + 1;

            var image = ListingImage.Create(
                listing.Id,
                storedImage.StorageKey,
                command.OriginalFileName,
                storedImage.ContentType,
                storedImage.SizeInBytes,
                sortOrder,
                timeProvider.GetUtcNow(),
                command.AltText);

            listing.AddImage(image, timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ListingOperationResult.Success(listing.Id);
        }
        catch (ArgumentException exception)
        {
            if (storedImage is not null)
            {
                await imageStorage.DeleteAsync(storedImage.StorageKey, cancellationToken).ConfigureAwait(false);
            }

            return ListingOperationResult.ValidationFailed(exception.Message);
        }
        catch (DomainException exception)
        {
            if (storedImage is not null)
            {
                await imageStorage.DeleteAsync(storedImage.StorageKey, cancellationToken).ConfigureAwait(false);
            }

            return ListingOperationResult.ValidationFailed(exception.Message);
        }
        catch (InvalidDataException exception)
        {
            return ListingOperationResult.ValidationFailed(exception.Message);
        }
    }

    public async Task<ListingImageFile?> OpenImageAsync(
        ListingImageId imageId,
        UserProfileId? actorId,
        bool canModerate,
        CancellationToken cancellationToken = default)
    {
        var image = await repository.GetImageAccessAsync(imageId, cancellationToken).ConfigureAwait(false);
        if (image is null)
        {
            return null;
        }

        var canReadPrivateImage = actorId == image.OwnerId || canModerate;
        if (!canReadPrivateImage && !IsPubliclyVisible(image.ListingStatus, image.ExpiresAt, timeProvider.GetUtcNow()))
        {
            return null;
        }

        return await imageStorage.OpenAsync(image, cancellationToken).ConfigureAwait(false);
    }

    public Task<int> ExpirePublishedAsync(CancellationToken cancellationToken = default)
        => repository.ExpirePublishedAsync(timeProvider.GetUtcNow(), cancellationToken);

    private async Task<ListingOperationResult?> CheckRateLimitAsync(
        ListingRateLimitAction action,
        UserProfileId actorId,
        ListingRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var lease = await rateLimiter.TryAcquireAsync(
            new ListingRateLimitRequest(action, actorId, requestContext),
            cancellationToken).ConfigureAwait(false);

        return lease.IsAllowed ? null : ListingOperationResult.RateLimited(lease.RetryAfter);
    }

    private static bool IsPubliclyVisible(
        ListingStatus status,
        DateTimeOffset? expiresAt,
        DateTimeOffset now)
        => status == ListingStatus.Published && (!expiresAt.HasValue || expiresAt.Value > now);
}
