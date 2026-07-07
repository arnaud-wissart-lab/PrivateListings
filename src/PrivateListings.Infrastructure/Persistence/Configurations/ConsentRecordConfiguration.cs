using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Domain.Privacy;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> builder)
    {
        builder.ToTable("consent_records");

        builder.HasKey(consentRecord => consentRecord.Id);

        builder.Property(consentRecord => consentRecord.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.ConsentRecordIdConverter)
            .ValueGeneratedNever();

        builder.Property(consentRecord => consentRecord.UserProfileId)
            .HasColumnName("user_profile_id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .IsRequired();

        builder.Property(consentRecord => consentRecord.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(consentRecord => consentRecord.IsGranted)
            .HasColumnName("is_granted")
            .IsRequired();

        builder.Property(consentRecord => consentRecord.PolicyVersion)
            .HasColumnName("policy_version")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(consentRecord => consentRecord.Source)
            .HasColumnName("source")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(consentRecord => consentRecord.RecordedAt)
            .HasColumnName("recorded_at")
            .IsRequired();

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany(userProfile => userProfile.ConsentHistory)
            .HasForeignKey(consentRecord => consentRecord.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(consentRecord => new { consentRecord.UserProfileId, consentRecord.Type, consentRecord.RecordedAt })
            .HasDatabaseName("ix_consent_records_user_type_recorded_at");
    }
}
