using Microsoft.Extensions.Options;
using PrivateListings.Application.Listings;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Application.Tests;

public sealed class ListingServiceTests
{
    private static readonly DateTimeOffset ReferenceDate = new(2026, 7, 7, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsyncCreeUneAnnonceEnBrouillon()
    {
        var ownerId = UserProfileId.New();
        var categoryId = ListingCategoryId.New();
        var repository = new FakeListingRepository(categoryId);
        var service = CreateService(repository);

        var result = await service.CreateAsync(new CreateListingCommand(
            ownerId,
            categoryId,
            "Table basse",
            "Table basse en bon état, disponible près de la gare.",
            ListingType.Sell,
            "Quartier gare",
            25m,
            null,
            ReferenceDate.AddDays(30),
            CreateRequestContext()));

        Assert.True(result.Succeeded);
        var listing = Assert.Single(repository.Listings);
        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal("Quartier gare", listing.ApproximateLocation);
        Assert.Equal(25m, listing.Price);
    }

    [Fact]
    public async Task UpdateAsyncAutoriseLeProprietaire()
    {
        var ownerId = UserProfileId.New();
        var categoryId = ListingCategoryId.New();
        var listing = CreateListing(ownerId, categoryId);
        var repository = new FakeListingRepository(categoryId, listing);
        var service = CreateService(repository);

        var result = await service.UpdateAsync(new UpdateListingCommand(
            ownerId,
            listing.Id,
            categoryId,
            "Titre modifié",
            "Description modifiée avec assez de contenu.",
            ListingType.Service,
            "Centre-ville",
            null,
            null,
            null));

        Assert.True(result.Succeeded);
        Assert.Equal("Titre modifié", listing.Title);
        Assert.Equal(ListingType.Service, listing.Type);
    }

    [Fact]
    public async Task UpdateAsyncRefuseUnAutreUtilisateur()
    {
        var ownerId = UserProfileId.New();
        var categoryId = ListingCategoryId.New();
        var listing = CreateListing(ownerId, categoryId);
        var repository = new FakeListingRepository(categoryId, listing);
        var service = CreateService(repository);

        var result = await service.UpdateAsync(new UpdateListingCommand(
            UserProfileId.New(),
            listing.Id,
            categoryId,
            "Titre refusé",
            "Description refusée avec assez de contenu.",
            ListingType.Service,
            "Centre-ville",
            null,
            null,
            null));

        Assert.Equal(ListingOperationStatus.Forbidden, result.Status);
        Assert.NotEqual("Titre refusé", listing.Title);
    }

    [Fact]
    public async Task PublishAsyncPublieUneAnnonceDuProprietaire()
    {
        var ownerId = UserProfileId.New();
        var categoryId = ListingCategoryId.New();
        var listing = CreateListing(ownerId, categoryId);
        var service = CreateService(new FakeListingRepository(categoryId, listing));

        var result = await service.PublishAsync(listing.Id, ownerId);

        Assert.True(result.Succeeded);
        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.Equal(ReferenceDate, listing.PublishedAt);
    }

    [Fact]
    public async Task DeleteAsyncEffectueUneSuppressionLogique()
    {
        var ownerId = UserProfileId.New();
        var categoryId = ListingCategoryId.New();
        var listing = CreateListing(ownerId, categoryId);
        var service = CreateService(new FakeListingRepository(categoryId, listing));

        var result = await service.DeleteAsync(listing.Id, ownerId);

        Assert.True(result.Succeeded);
        Assert.Equal(ListingStatus.Deleted, listing.Status);
        Assert.Equal(ReferenceDate, listing.DeletedAt);
    }

    [Fact]
    public async Task ExpirePublishedAsyncExpireLesAnnoncesPubliees()
    {
        var ownerId = UserProfileId.New();
        var categoryId = ListingCategoryId.New();
        var listing = Listing.Create(
            ownerId,
            categoryId,
            "Annonce courte",
            "Annonce destinée au test d'expiration.",
            ListingType.Give,
            ReferenceDate.AddDays(-2),
            ReferenceDate.AddHours(-1),
            "Quartier test");
        listing.Publish(ReferenceDate.AddDays(-1));
        var repository = new FakeListingRepository(categoryId, listing);
        var service = CreateService(repository);

        var expiredCount = await service.ExpirePublishedAsync();

        Assert.Equal(1, expiredCount);
        Assert.Equal(ListingStatus.Expired, listing.Status);
    }

    private static ListingService CreateService(FakeListingRepository repository)
        => new(
            repository,
            new FakeListingImageStorage(),
            new AllowingListingRateLimiter(),
            Options.Create(new ListingImageOptions()),
            new FixedTimeProvider(ReferenceDate));

    private static Listing CreateListing(UserProfileId ownerId, ListingCategoryId categoryId)
        => Listing.Create(
            ownerId,
            categoryId,
            "Tondeuse à prêter",
            "Disponible le week-end pour les voisins proches.",
            ListingType.Give,
            ReferenceDate.AddDays(-1),
            ReferenceDate.AddDays(30),
            "Quartier test");

    private static ListingRequestContext CreateRequestContext()
        => new("127.0.0.1", "PrivateListings.Tests");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class AllowingListingRateLimiter : IListingRateLimiter
    {
        public Task<ListingRateLimitLease> TryAcquireAsync(
            ListingRateLimitRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ListingRateLimitLease(true));
    }

    private sealed class FakeListingImageStorage : IListingImageStorage
    {
        public Task<StoredListingImage> SaveAsync(
            ListingImageUpload upload,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new StoredListingImage("tests/image.jpg", "image/jpeg", 128));

        public Task<ListingImageFile?> OpenAsync(
            ListingImageAccess image,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ListingImageFile?>(null);

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeListingRepository(ListingCategoryId categoryId, params Listing[] listings)
        : IListingRepository
    {
        public List<Listing> Listings { get; } = listings.ToList();

        public Task<IReadOnlyList<ListingCategoryOption>> ListActiveCategoriesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ListingCategoryOption>>(
                [new ListingCategoryOption(categoryId, "Tests", null)]);

        public Task<bool> ActiveCategoryExistsAsync(
            ListingCategoryId requestedCategoryId,
            CancellationToken cancellationToken)
            => Task.FromResult(requestedCategoryId == categoryId);

        public Task<IReadOnlyList<ListingSummary>> SearchPublishedAsync(
            ListingSearchQuery query,
            DateTimeOffset now,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ListingSummary>> ListOwnerListingsAsync(
            UserProfileId ownerId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PrivateListings.Application.Listings.ListingDetails?> GetPublicDetailsAsync(
            ListingId listingId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PrivateListings.Application.Listings.ListingDetails?> GetDetailsAsync(
            ListingId listingId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Listing?> GetForUpdateAsync(
            ListingId listingId,
            CancellationToken cancellationToken)
            => Task.FromResult(Listings.SingleOrDefault(listing => listing.Id == listingId));

        public Task<ListingImageAccess?> GetImageAccessAsync(
            ListingImageId imageId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task AddAsync(Listing listing, CancellationToken cancellationToken)
        {
            Listings.Add(listing);
            return Task.CompletedTask;
        }

        public Task<int> ExpirePublishedAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            var expiredCount = 0;
            foreach (var listing in Listings)
            {
                if (listing.ExpireIfNeeded(now))
                {
                    expiredCount++;
                }
            }

            return Task.FromResult(expiredCount);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
