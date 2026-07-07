using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PrivateListings.Application.Listings;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;
using PrivateListings.Domain.Users;
using PrivateListings.Infrastructure.Persistence;
using PrivateListings.Infrastructure.Storage;

namespace PrivateListings.IntegrationTests;

[Collection(PostgreSqlTestCollectionDefinition.Name)]
public sealed class ListingModuleIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ExpirationAutomatiqueMarqueAnnonceExpiree()
    {
        await fixture.ResetDatabaseAsync();
        await using var serviceProvider = fixture.CreateServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var listingService = scope.ServiceProvider.GetRequiredService<IListingService>();
        var now = DateTimeOffset.UtcNow;
        var owner = CreateUser("expiration@example.test", "Expiration Test", now.AddDays(-4));
        var category = ListingCategory.Create("Expiration", now.AddDays(-4));
        var listing = Listing.Create(
            owner.Id,
            category.Id,
            "Annonce à expirer",
            "Annonce publiée dont la date d'expiration est dépassée.",
            ListingType.Give,
            now.AddDays(-3),
            now.AddDays(-1),
            "Quartier test");
        listing.Publish(now.AddDays(-2));
        dbContext.UserProfiles.Add(owner);
        dbContext.ListingCategories.Add(category);
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        var expiredCount = await listingService.ExpirePublishedAsync();

        dbContext.ChangeTracker.Clear();
        var persisted = await dbContext.Listings.SingleAsync(value => value.Id == listing.Id);
        Assert.Equal(1, expiredCount);
        Assert.Equal(ListingStatus.Expired, persisted.Status);
    }

    [Fact]
    public async Task AnnonceExpireeNestPasVisibleDansLaListePublique()
    {
        await fixture.ResetDatabaseAsync();
        await using var serviceProvider = fixture.CreateServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var listingService = scope.ServiceProvider.GetRequiredService<IListingService>();
        var now = DateTimeOffset.UtcNow;
        var owner = CreateUser("expiree@example.test", "Annonce Expirée", now.AddDays(-4));
        var category = ListingCategory.Create("Visibilité", now.AddDays(-4));
        var listing = Listing.Create(
            owner.Id,
            category.Id,
            "Annonce expirée invisible",
            "Annonce publiée qui ne doit pas apparaître dans la liste publique.",
            ListingType.Event,
            now.AddDays(-3),
            now.AddDays(-1),
            "Quartier test",
            null,
            now.AddDays(-2));
        listing.Publish(now.AddDays(-2).AddHours(-1));
        dbContext.UserProfiles.Add(owner);
        dbContext.ListingCategories.Add(category);
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        var listings = await listingService.SearchPublishedAsync(new ListingSearchQuery(Text: "invisible"));

        Assert.DoesNotContain(listings, value => value.Id == listing.Id);
    }

    [Fact]
    public async Task AnnonceSuspendueNestPasVisibleDansLaListePublique()
    {
        await fixture.ResetDatabaseAsync();
        await using var serviceProvider = fixture.CreateServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var listingService = scope.ServiceProvider.GetRequiredService<IListingService>();
        var now = DateTimeOffset.UtcNow;
        var owner = CreateUser("suspendue@example.test", "Annonce Suspendue", now.AddDays(-2));
        var category = ListingCategory.Create("Modération", now.AddDays(-2));
        var listing = Listing.Create(
            owner.Id,
            category.Id,
            "Annonce suspendue invisible",
            "Annonce publiée qui doit disparaître après suspension.",
            ListingType.Service,
            now.AddDays(-2),
            now.AddDays(10),
            "Quartier test");
        listing.Publish(now.AddDays(-2).AddHours(1));
        listing.Suspend("Test de suspension", now.AddDays(-1));
        dbContext.UserProfiles.Add(owner);
        dbContext.ListingCategories.Add(category);
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        var listings = await listingService.SearchPublishedAsync(new ListingSearchQuery(Text: "suspendue invisible"));

        Assert.DoesNotContain(listings, value => value.Id == listing.Id);
    }

    private static UserProfile CreateUser(string email, string displayName, DateTimeOffset createdAt)
        => UserProfile.Create(
            EmailAddress.Create(email),
            displayName,
            createdAt,
            "Profil fictif créé par un test d'intégration.");
}

public sealed class ListingImageStorageTests : IDisposable
{
    private readonly string rootPath = Path.Combine(Path.GetTempPath(), "PrivateListings.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsyncAccepteUneImagePngValide()
    {
        var storage = CreateStorage();
        var content = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

        var stored = await storage.SaveAsync(new ListingImageUpload(
            "image-test.png",
            "image/png",
            new MemoryStream(content),
            content.Length));

        var opened = await storage.OpenAsync(new ListingImageAccess(
            ListingImageId.New(),
            ListingId.New(),
            UserProfileId.New(),
            ListingStatus.Published,
            null,
            stored.StorageKey,
            stored.ContentType,
            "image-test.png",
            DateTimeOffset.UtcNow));

        Assert.Equal("image/png", stored.ContentType);
        Assert.NotNull(opened);
        await opened.Content.DisposeAsync();
    }

    [Fact]
    public async Task SaveAsyncRefuseUnTypeInvalide()
    {
        var storage = CreateStorage();
        var content = "Ceci n'est pas une image."u8.ToArray();

        await Assert.ThrowsAsync<InvalidDataException>(() => storage.SaveAsync(new ListingImageUpload(
            "image-test.png",
            "text/plain",
            new MemoryStream(content),
            content.Length)));
    }

    [Fact]
    public async Task SaveAsyncRefuseLesSvg()
    {
        var storage = CreateStorage();
        var content = "<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"u8.ToArray();

        await Assert.ThrowsAsync<InvalidDataException>(() => storage.SaveAsync(new ListingImageUpload(
            "image-test.svg",
            "image/svg+xml",
            new MemoryStream(content),
            content.Length)));
    }

    [Fact]
    public async Task SaveAsyncRefuseUneImageTropGrande()
    {
        var storage = CreateStorage(maxSizeInBytes: 8);
        var content = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

        await Assert.ThrowsAsync<InvalidDataException>(() => storage.SaveAsync(new ListingImageUpload(
            "image-test.png",
            "image/png",
            new MemoryStream(content),
            content.Length)));
    }


    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private FileSystemListingImageStorage CreateStorage(long maxSizeInBytes = 1024 * 1024)
        => new(
            Options.Create(new ListingImageOptions
            {
                MaxSizeInBytes = maxSizeInBytes,
                MaxImagesPerListing = 4,
                StorageRoot = Path.Combine(rootPath, "images")
            }),
            new TestHostEnvironment("Tests")
            {
                ContentRootPath = rootPath
            },
            NullLogger<FileSystemListingImageStorage>.Instance);
}
