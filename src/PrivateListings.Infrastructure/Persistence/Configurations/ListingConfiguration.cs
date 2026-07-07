using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable(
            "listings",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_listings_expires_after_created",
                    "expires_at IS NULL OR expires_at > created_at");

                table.HasCheckConstraint(
                    "ck_listings_published_after_created",
                    "published_at IS NULL OR published_at >= created_at");

                table.HasCheckConstraint(
                    "ck_listings_price_positive",
                    "price IS NULL OR price >= 0");
            });

        builder.HasKey(listing => listing.Id);

        builder.Property(listing => listing.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.ListingIdConverter)
            .ValueGeneratedNever();

        builder.Property(listing => listing.OwnerId)
            .HasColumnName("owner_id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .IsRequired();

        builder.Property(listing => listing.CategoryId)
            .HasColumnName("category_id")
            .HasConversion(StronglyTypedIdConverters.ListingCategoryIdConverter)
            .IsRequired();

        builder.Property(listing => listing.Title)
            .HasColumnName("title")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(listing => listing.Description)
            .HasColumnName("description")
            .HasMaxLength(4_000)
            .IsRequired();

        builder.Property(listing => listing.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(listing => listing.ApproximateLocation)
            .HasColumnName("approximate_location")
            .HasMaxLength(120);

        builder.Property(listing => listing.Price)
            .HasColumnName("price")
            .HasPrecision(12, 2);

        builder.Property(listing => listing.EventDate)
            .HasColumnName("event_date");

        builder.Property(listing => listing.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(listing => listing.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(listing => listing.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(listing => listing.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(listing => listing.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(listing => listing.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(listing => listing.SuspendedAt)
            .HasColumnName("suspended_at");

        builder.Property(listing => listing.SuspensionReason)
            .HasColumnName("suspension_reason")
            .HasMaxLength(500);

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany()
            .HasForeignKey(listing => listing.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ListingCategory>()
            .WithMany()
            .HasForeignKey(listing => listing.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(listing => listing.Images)
            .WithOne()
            .HasForeignKey(image => image.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(listing => listing.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(listing => new { listing.OwnerId, listing.Status })
            .HasDatabaseName("ix_listings_owner_status");

        builder.HasIndex(listing => new { listing.CategoryId, listing.Status })
            .HasDatabaseName("ix_listings_category_status");

        builder.HasIndex(listing => new { listing.Type, listing.Status })
            .HasDatabaseName("ix_listings_type_status");

        builder.HasIndex(listing => new { listing.Status, listing.ExpiresAt })
            .HasDatabaseName("ix_listings_status_expires_at");

        builder.HasIndex(listing => new { listing.Status, listing.PublishedAt })
            .HasDatabaseName("ix_listings_status_published_at");

        builder.HasIndex(listing => listing.CreatedAt)
            .HasDatabaseName("ix_listings_created_at");
    }
}
