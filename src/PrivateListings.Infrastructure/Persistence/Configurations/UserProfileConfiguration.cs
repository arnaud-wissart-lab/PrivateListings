using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrivateListings.Domain.Users;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    private static readonly ValueConverter<EmailAddress?, string?> EmailAddressConverter =
        new(emailAddress => emailAddress.HasValue ? emailAddress.Value.Value : null, value => value == null ? null : EmailAddress.Create(value));

    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(userProfile => userProfile.Id);

        builder.Property(userProfile => userProfile.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .ValueGeneratedNever();

        builder.Property(userProfile => userProfile.EmailAddress)
            .HasColumnName("email_address")
            .HasConversion(EmailAddressConverter)
            .HasMaxLength(254);

        builder.Property(userProfile => userProfile.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(userProfile => userProfile.Bio)
            .HasColumnName("bio")
            .HasMaxLength(1_000);

        builder.Property(userProfile => userProfile.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(userProfile => userProfile.AnonymizedAt)
            .HasColumnName("anonymized_at");

        builder.HasMany(userProfile => userProfile.ConsentHistory)
            .WithOne()
            .HasForeignKey(consentRecord => consentRecord.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(userProfile => userProfile.ConsentHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(userProfile => userProfile.EmailAddress)
            .IsUnique()
            .HasFilter("email_address IS NOT NULL")
            .HasDatabaseName("ux_user_profiles_email_address");

        builder.HasIndex(userProfile => userProfile.CreatedAt)
            .HasDatabaseName("ix_user_profiles_created_at");

        builder.HasIndex(userProfile => userProfile.AnonymizedAt)
            .HasDatabaseName("ix_user_profiles_anonymized_at");
    }
}
