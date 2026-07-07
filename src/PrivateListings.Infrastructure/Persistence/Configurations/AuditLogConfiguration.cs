using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrivateListings.Domain.Auditing;
using PrivateListings.Domain.Common;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    private static readonly ValueConverter<AuditLogSubject, string> AuditLogSubjectConverter =
        new(subject => FormatSubject(subject), value => ParseSubject(value));

    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.AuditLogIdConverter)
            .ValueGeneratedNever();

        builder.Property(auditLog => auditLog.ActorUserProfileId)
            .HasColumnName("actor_user_profile_id")
            .HasConversion(StronglyTypedIdConverters.NullableUserProfileIdConverter);

        builder.Property(auditLog => auditLog.Action)
            .HasColumnName("action")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(auditLog => auditLog.Subject)
            .HasColumnName("subject_key")
            .HasConversion(AuditLogSubjectConverter)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(auditLog => auditLog.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(auditLog => auditLog.Metadata)
            .HasColumnName("metadata")
            .HasMaxLength(4_000);

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany()
            .HasForeignKey(auditLog => auditLog.ActorUserProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(auditLog => auditLog.ActorUserProfileId)
            .HasDatabaseName("ix_audit_logs_actor_user_profile_id");

        builder.HasIndex(auditLog => auditLog.OccurredAt)
            .HasDatabaseName("ix_audit_logs_occurred_at");

        builder.HasIndex(auditLog => auditLog.Action)
            .HasDatabaseName("ix_audit_logs_action");

        builder.HasIndex(auditLog => auditLog.Subject)
            .HasDatabaseName("ix_audit_logs_subject_key");
    }

    private static string FormatSubject(AuditLogSubject subject)
        => $"{subject.Type}|{subject.EntityId?.ToString("D") ?? string.Empty}";

    private static AuditLogSubject ParseSubject(string value)
    {
        var parts = value.Split('|', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException("La clé de sujet d'audit est invalide.");
        }

        var entityId = string.IsNullOrWhiteSpace(parts[1]) ? (Guid?)null : Guid.Parse(parts[1]);

        return parts[0] switch
        {
            "Listing" when entityId.HasValue => AuditLogSubject.ForListing(ListingId.From(entityId.Value)),
            "Message" when entityId.HasValue => AuditLogSubject.ForMessage(MessageId.From(entityId.Value)),
            "UserProfile" when entityId.HasValue => AuditLogSubject.ForUserProfile(UserProfileId.From(entityId.Value)),
            _ when !entityId.HasValue => AuditLogSubject.ForSystem(parts[0]),
            _ => throw new InvalidOperationException("La clé de sujet d'audit ne correspond à aucun sujet connu.")
        };
    }
}
