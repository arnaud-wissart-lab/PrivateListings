using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Infrastructure.Persistence.Seeding;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class DemoUserAccountConfiguration : IEntityTypeConfiguration<DemoUserAccount>
{
    public void Configure(EntityTypeBuilder<DemoUserAccount> builder)
    {
        builder.ToTable("demo_user_accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(account => account.UserProfileId)
            .HasColumnName("user_profile_id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .IsRequired();

        builder.Property(account => account.Email)
            .HasColumnName("email")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(account => account.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(account => account.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(account => account.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne<Domain.Users.UserProfile>()
            .WithOne()
            .HasForeignKey<DemoUserAccount>(account => account.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(account => account.Email)
            .IsUnique()
            .HasDatabaseName("ux_demo_user_accounts_email");

        builder.HasIndex(account => account.UserProfileId)
            .IsUnique()
            .HasDatabaseName("ux_demo_user_accounts_user_profile_id");

        builder.HasIndex(account => account.Role)
            .HasDatabaseName("ix_demo_user_accounts_role");
    }
}
