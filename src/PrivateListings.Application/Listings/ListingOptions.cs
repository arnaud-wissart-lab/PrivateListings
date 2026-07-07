namespace PrivateListings.Application.Listings;

public sealed class ListingImageOptions
{
    public const string SectionName = "PrivateListings:ListingImages";

    public long MaxSizeInBytes { get; set; } = 5 * 1024 * 1024;

    public int MaxImagesPerListing { get; set; } = 8;

    public string? StorageRoot { get; set; }
}

public sealed class ListingRateLimitOptions
{
    public const string SectionName = "PrivateListings:ListingRateLimits";

    public int CreatePermitLimit { get; set; } = 10;

    public int UploadPermitLimit { get; set; } = 30;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan BlockDuration { get; set; } = TimeSpan.FromMinutes(10);
}
