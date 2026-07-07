using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class ListingImageConfiguration : IEntityTypeConfiguration<ListingImage>
{
    public void Configure(EntityTypeBuilder<ListingImage> builder)
    {
        builder.ToTable(
            "listing_images",
            table =>
            {
                table.HasCheckConstraint("ck_listing_images_size_positive", "size_in_bytes > 0");
                table.HasCheckConstraint("ck_listing_images_sort_order_positive", "sort_order >= 0");
            });

        builder.HasKey(image => image.Id);

        builder.Property(image => image.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.ListingImageIdConverter)
            .ValueGeneratedNever();

        builder.Property(image => image.ListingId)
            .HasColumnName("listing_id")
            .HasConversion(StronglyTypedIdConverters.ListingIdConverter)
            .IsRequired();

        builder.Property(image => image.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(image => image.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(image => image.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(image => image.SizeInBytes)
            .HasColumnName("size_in_bytes")
            .IsRequired();

        builder.Property(image => image.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(image => image.UploadedAt)
            .HasColumnName("uploaded_at")
            .IsRequired();

        builder.Property(image => image.AltText)
            .HasColumnName("alt_text")
            .HasMaxLength(160);

        builder.HasIndex(image => image.StorageKey)
            .IsUnique()
            .HasDatabaseName("ux_listing_images_storage_key");

        builder.HasIndex(image => new { image.ListingId, image.SortOrder })
            .IsUnique()
            .HasDatabaseName("ux_listing_images_listing_sort_order");
    }
}
