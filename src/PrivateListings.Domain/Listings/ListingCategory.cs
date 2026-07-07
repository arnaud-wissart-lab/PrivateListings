using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Listings;

public sealed class ListingCategory
{
    private ListingCategory(
        ListingCategoryId id,
        string name,
        DateTimeOffset createdAt,
        string? description)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));

        Id = id;
        Name = Guard.AgainstBlank(name, nameof(name), 80);
        Description = Guard.OptionalText(description, nameof(description), 500);
        CreatedAt = Guard.AgainstDefault(createdAt, nameof(createdAt));
        IsActive = true;
    }

    public ListingCategoryId Id { get; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public static ListingCategory Create(string name, DateTimeOffset createdAt, string? description = null)
        => new(ListingCategoryId.New(), name, createdAt, description);

    public void Rename(string name, string? description = null)
    {
        Name = Guard.AgainstBlank(name, nameof(name), 80);
        Description = Guard.OptionalText(description, nameof(description), 500);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
