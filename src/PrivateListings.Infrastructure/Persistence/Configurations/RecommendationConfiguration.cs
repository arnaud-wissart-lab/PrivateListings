using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Domain.Recommendations;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable(
            "recommendations",
            table =>
            {
                table.HasCheckConstraint("ck_recommendations_score_range", "score >= 0 AND score <= 1");
            });

        builder.HasKey(recommendation => recommendation.Id);

        builder.Property(recommendation => recommendation.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.RecommendationIdConverter)
            .ValueGeneratedNever();

        builder.Property(recommendation => recommendation.UserProfileId)
            .HasColumnName("user_profile_id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .IsRequired();

        builder.Property(recommendation => recommendation.ListingId)
            .HasColumnName("listing_id")
            .HasConversion(StronglyTypedIdConverters.ListingIdConverter)
            .IsRequired();

        builder.Property(recommendation => recommendation.Score)
            .HasColumnName("score")
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(recommendation => recommendation.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500);

        builder.Property(recommendation => recommendation.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(recommendation => recommendation.DismissedAt)
            .HasColumnName("dismissed_at");

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany()
            .HasForeignKey(recommendation => recommendation.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Listings.Listing>()
            .WithMany()
            .HasForeignKey(recommendation => recommendation.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(recommendation => new { recommendation.UserProfileId, recommendation.ListingId })
            .IsUnique()
            .HasDatabaseName("ux_recommendations_user_listing");

        builder.HasIndex(recommendation => recommendation.ListingId)
            .HasDatabaseName("ix_recommendations_listing_id");

        builder.HasIndex(recommendation => new { recommendation.UserProfileId, recommendation.Score })
            .HasDatabaseName("ix_recommendations_user_score");
    }
}
