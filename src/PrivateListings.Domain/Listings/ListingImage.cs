using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Listings;

public sealed class ListingImage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private ListingImage()
    {
        StorageKey = string.Empty;
        OriginalFileName = string.Empty;
        ContentType = string.Empty;
    }

    private ListingImage(
        ListingImageId id,
        ListingId listingId,
        string storageKey,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        int sortOrder,
        DateTimeOffset uploadedAt,
        string? altText)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));
        Guard.AgainstEmpty(listingId.Value, nameof(listingId));

        if (sizeInBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "La taille du fichier doit être positive.");
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "L'ordre d'affichage ne peut pas être négatif.");
        }

        var normalizedContentType = Guard.AgainstBlank(contentType, nameof(contentType), 100).ToLowerInvariant();
        if (!AllowedContentTypes.Contains(normalizedContentType))
        {
            throw new ArgumentException("Le type de fichier image n'est pas autorisé.", nameof(contentType));
        }

        Id = id;
        ListingId = listingId;
        StorageKey = Guard.AgainstBlank(storageKey, nameof(storageKey), 260);
        OriginalFileName = Guard.AgainstBlank(originalFileName, nameof(originalFileName), 255);
        ContentType = normalizedContentType;
        SizeInBytes = sizeInBytes;
        SortOrder = sortOrder;
        UploadedAt = Guard.AgainstDefault(uploadedAt, nameof(uploadedAt));
        AltText = Guard.OptionalText(altText, nameof(altText), 160);
    }

    public ListingImageId Id { get; }

    public ListingId ListingId { get; }

    public string StorageKey { get; }

    public string OriginalFileName { get; }

    public string ContentType { get; }

    public long SizeInBytes { get; }

    public int SortOrder { get; private set; }

    public DateTimeOffset UploadedAt { get; }

    public string? AltText { get; private set; }

    public static ListingImage Create(
        ListingId listingId,
        string storageKey,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        int sortOrder,
        DateTimeOffset uploadedAt,
        string? altText = null)
        => new(
            ListingImageId.New(),
            listingId,
            storageKey,
            originalFileName,
            contentType,
            sizeInBytes,
            sortOrder,
            uploadedAt,
            altText);

    public void Reorder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "L'ordre d'affichage ne peut pas être négatif.");
        }

        SortOrder = sortOrder;
    }

    public void UpdateAltText(string? altText)
        => AltText = Guard.OptionalText(altText, nameof(altText), 160);
}
