using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Domain.Privacy;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class DataExportRequestConfiguration : IEntityTypeConfiguration<DataExportRequest>
{
    public void Configure(EntityTypeBuilder<DataExportRequest> builder)
    {
        builder.ToTable(
            "data_export_requests",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_data_export_requests_download_expires_after_completed",
                    "download_expires_at IS NULL OR completed_at IS NULL OR download_expires_at > completed_at");
            });

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.DataExportRequestIdConverter)
            .ValueGeneratedNever();

        builder.Property(request => request.UserProfileId)
            .HasColumnName("user_profile_id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(request => request.RequestedAt)
            .HasColumnName("requested_at")
            .IsRequired();

        builder.Property(request => request.ProcessingStartedAt)
            .HasColumnName("processing_started_at");

        builder.Property(request => request.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(request => request.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(request => request.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(260);

        builder.Property(request => request.DownloadExpiresAt)
            .HasColumnName("download_expires_at");

        builder.Property(request => request.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1_000);

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany()
            .HasForeignKey(request => request.UserProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(request => new { request.UserProfileId, request.RequestedAt })
            .HasDatabaseName("ix_data_export_requests_user_requested_at");

        builder.HasIndex(request => request.Status)
            .HasDatabaseName("ix_data_export_requests_status");
    }
}
