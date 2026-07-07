using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Domain.Listings;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class ListingCategoryConfiguration : IEntityTypeConfiguration<ListingCategory>
{
    public void Configure(EntityTypeBuilder<ListingCategory> builder)
    {
        builder.ToTable("listing_categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.ListingCategoryIdConverter)
            .ValueGeneratedNever();

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(category => category.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(category => category.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique()
            .HasDatabaseName("ux_listing_categories_name");

        builder.HasIndex(category => category.IsActive)
            .HasDatabaseName("ix_listing_categories_is_active");
    }
}
