using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Listings;

public sealed class Listing
{
    private readonly List<ListingImage> _images = [];

    private Listing()
    {
        Title = string.Empty;
        Description = string.Empty;
    }

    private Listing(
        ListingId id,
        UserProfileId ownerId,
        ListingCategoryId categoryId,
        string title,
        string description,
        ListingType type,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));
        Guard.AgainstEmpty(ownerId.Value, nameof(ownerId));
        Guard.AgainstEmpty(categoryId.Value, nameof(categoryId));

        Id = id;
        OwnerId = ownerId;
        CategoryId = categoryId;
        Title = Guard.AgainstBlank(title, nameof(title), 160);
        Description = Guard.AgainstBlank(description, nameof(description), 4_000);
        Type = type;
        CreatedAt = Guard.AgainstDefault(createdAt, nameof(createdAt));
        ExpiresAt = expiresAt;
        Status = ListingStatus.Draft;
    }

    public ListingId Id { get; }

    public UserProfileId OwnerId { get; }

    public ListingCategoryId CategoryId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public ListingType Type { get; private set; }

    public ListingStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset? SuspendedAt { get; private set; }

    public string? SuspensionReason { get; private set; }

    public IReadOnlyCollection<ListingImage> Images => _images.AsReadOnly();

    public static Listing Create(
        UserProfileId ownerId,
        ListingCategoryId categoryId,
        string title,
        string description,
        ListingType type,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt = null)
        => new(ListingId.New(), ownerId, categoryId, title, description, type, createdAt, expiresAt);

    public ListingStatus GetStatusAt(DateTimeOffset referenceDate)
    {
        if (Status == ListingStatus.Published && ExpiresAt.HasValue && ExpiresAt.Value <= referenceDate)
        {
            return ListingStatus.Expired;
        }

        return Status;
    }

    public void UpdateDetails(
        ListingCategoryId categoryId,
        string title,
        string description,
        ListingType type,
        DateTimeOffset updatedAt,
        DateTimeOffset? expiresAt = null)
    {
        EnsureEditable();
        Guard.AgainstEmpty(categoryId.Value, nameof(categoryId));

        CategoryId = categoryId;
        Title = Guard.AgainstBlank(title, nameof(title), 160);
        Description = Guard.AgainstBlank(description, nameof(description), 4_000);
        Type = type;
        ExpiresAt = expiresAt;
        UpdatedAt = Guard.AgainstDefault(updatedAt, nameof(updatedAt));

        ExpireIfNeeded(updatedAt);
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        if (Status is ListingStatus.Deleted or ListingStatus.Suspended)
        {
            throw new DomainException("Une annonce supprimée ou suspendue ne peut pas être publiée.");
        }

        var publicationDate = Guard.AgainstDefault(publishedAt, nameof(publishedAt));
        PublishedAt = publicationDate;
        UpdatedAt = publicationDate;
        Status = ListingStatus.Published;

        ExpireIfNeeded(publicationDate);
    }

    public bool ExpireIfNeeded(DateTimeOffset referenceDate)
    {
        if (Status != ListingStatus.Published || !ExpiresAt.HasValue || ExpiresAt.Value > referenceDate)
        {
            return false;
        }

        Status = ListingStatus.Expired;
        UpdatedAt = Guard.AgainstDefault(referenceDate, nameof(referenceDate));
        return true;
    }

    public void Delete(DateTimeOffset deletedAt)
    {
        if (Status == ListingStatus.Deleted)
        {
            return;
        }

        var deletionDate = Guard.AgainstDefault(deletedAt, nameof(deletedAt));
        Status = ListingStatus.Deleted;
        DeletedAt = deletionDate;
        UpdatedAt = deletionDate;
    }

    public void Suspend(string reason, DateTimeOffset suspendedAt)
    {
        if (Status == ListingStatus.Deleted)
        {
            throw new DomainException("Une annonce supprimée ne peut pas être suspendue.");
        }

        var suspensionDate = Guard.AgainstDefault(suspendedAt, nameof(suspendedAt));
        Status = ListingStatus.Suspended;
        SuspensionReason = Guard.AgainstBlank(reason, nameof(reason), 500);
        SuspendedAt = suspensionDate;
        UpdatedAt = suspensionDate;
    }

    public void AddImage(ListingImage image, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (image.ListingId != Id)
        {
            throw new DomainException("L'image doit appartenir à l'annonce.");
        }

        EnsureEditable();
        _images.Add(image);
        UpdatedAt = Guard.AgainstDefault(updatedAt, nameof(updatedAt));
    }

    private void EnsureEditable()
    {
        if (Status is ListingStatus.Deleted or ListingStatus.Suspended)
        {
            throw new DomainException("Une annonce supprimée ou suspendue ne peut pas être modifiée.");
        }
    }
}
