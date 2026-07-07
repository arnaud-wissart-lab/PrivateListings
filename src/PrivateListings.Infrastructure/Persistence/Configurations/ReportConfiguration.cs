using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrivateListings.Domain.Common;
using PrivateListings.Domain.Moderation;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    private static readonly ValueConverter<ReportTarget, string> ReportTargetConverter =
        new(target => FormatTarget(target), value => ParseTarget(value));

    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable(
            "reports",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_reports_reviewed_after_created",
                    "reviewed_at IS NULL OR reviewed_at >= created_at");
            });

        builder.HasKey(report => report.Id);

        builder.Property(report => report.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.ReportIdConverter)
            .ValueGeneratedNever();

        builder.Property(report => report.ReporterId)
            .HasColumnName("reporter_id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .IsRequired();

        builder.Property(report => report.Target)
            .HasColumnName("target_key")
            .HasConversion(ReportTargetConverter)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(report => report.Reason)
            .HasColumnName("reason")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(report => report.Details)
            .HasColumnName("details")
            .HasMaxLength(2_000);

        builder.Property(report => report.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(report => report.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(report => report.ReviewedAt)
            .HasColumnName("reviewed_at");

        builder.Property(report => report.ReviewedById)
            .HasColumnName("reviewed_by_id")
            .HasConversion(StronglyTypedIdConverters.NullableUserProfileIdConverter);

        builder.Property(report => report.ResolutionNote)
            .HasColumnName("resolution_note")
            .HasMaxLength(1_000);

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany()
            .HasForeignKey(report => report.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany()
            .HasForeignKey(report => report.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(report => new { report.Status, report.CreatedAt })
            .HasDatabaseName("ix_reports_status_created_at");

        builder.HasIndex(report => report.ReporterId)
            .HasDatabaseName("ix_reports_reporter_id");

        builder.HasIndex(report => report.ReviewedById)
            .HasDatabaseName("ix_reports_reviewed_by_id");

        builder.HasIndex(report => report.Target)
            .HasDatabaseName("ix_reports_target_key");
    }

    private static string FormatTarget(ReportTarget target)
        => $"{target.Type}|{target.TargetId:D}";

    private static ReportTarget ParseTarget(string value)
    {
        var parts = value.Split('|', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException("La clé de cible de signalement est invalide.");
        }

        var targetId = Guid.Parse(parts[1]);
        var targetType = Enum.Parse<ReportTargetType>(parts[0], ignoreCase: false);

        return targetType switch
        {
            ReportTargetType.Listing => ReportTarget.ForListing(ListingId.From(targetId)),
            ReportTargetType.Conversation => ReportTarget.ForConversation(ConversationId.From(targetId)),
            ReportTargetType.Message => ReportTarget.ForMessage(MessageId.From(targetId)),
            ReportTargetType.UserProfile => ReportTarget.ForUserProfile(UserProfileId.From(targetId)),
            _ => throw new InvalidOperationException("La clé de cible de signalement ne correspond à aucune cible connue.")
        };
    }
}
