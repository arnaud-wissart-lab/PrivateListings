using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrivateListings.Domain.Users;
using PrivateListings.Infrastructure.Identity;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal static class IdentityModelBuilderExtensions
{
    public static void ConfigurePrivateListingsIdentity(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrivateListingsUser>(builder =>
        {
            builder.ToTable("identity_users");

            builder.Property(user => user.UserProfileId)
                .HasColumnName("user_profile_id")
                .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
                .IsRequired();

            builder.Property(user => user.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(user => user.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(user => user.MfaRequiredForAdmin)
                .HasColumnName("mfa_required_for_admin")
                .IsRequired();

            builder.Property(user => user.SuspendedAt)
                .HasColumnName("suspended_at");

            builder.Property(user => user.SuspensionReason)
                .HasColumnName("suspension_reason")
                .HasMaxLength(500);

            builder.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(user => user.UserProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(user => user.UserProfileId)
                .IsUnique()
                .HasDatabaseName("ux_identity_users_user_profile_id");

            builder.HasIndex(user => user.SuspendedAt)
                .HasDatabaseName("ix_identity_users_suspended_at");
        });

        modelBuilder.Entity<PrivateListingsRole>()
            .ToTable("identity_roles");

        modelBuilder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("identity_role_claims");

        modelBuilder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("identity_user_claims");

        modelBuilder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("identity_user_logins");

        modelBuilder.Entity<IdentityUserRole<Guid>>()
            .ToTable("identity_user_roles");

        modelBuilder.Entity<IdentityUserToken<Guid>>()
            .ToTable("identity_user_tokens");
    }
}
