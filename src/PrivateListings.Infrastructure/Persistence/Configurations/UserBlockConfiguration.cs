using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Domain.Messaging;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable(
            "user_blocks",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_user_blocks_distinct_users",
                    "blocker_id <> blocked_id");

                table.HasCheckConstraint(
                    "ck_user_blocks_revoked_after_created",
                    "revoked_at IS NULL OR revoked_at >= created_at");
            });

        builder.HasKey(userBlock => new { userBlock.BlockerId, userBlock.BlockedId });

        builder.Property(userBlock => userBlock.BlockerId)
            .HasColumnName("blocker_id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .IsRequired();

        builder.Property(userBlock => userBlock.BlockedId)
            .HasColumnName("blocked_id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .IsRequired();

        builder.Property(userBlock => userBlock.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(userBlock => userBlock.RevokedAt)
            .HasColumnName("revoked_at");

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany()
            .HasForeignKey(userBlock => userBlock.BlockerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany()
            .HasForeignKey(userBlock => userBlock.BlockedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(userBlock => new { userBlock.BlockedId, userBlock.RevokedAt })
            .HasDatabaseName("ix_user_blocks_blocked_active");
    }
}
