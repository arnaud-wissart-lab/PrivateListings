using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Domain.Messaging;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class ConversationReadStateConfiguration : IEntityTypeConfiguration<ConversationReadState>
{
    public void Configure(EntityTypeBuilder<ConversationReadState> builder)
    {
        builder.ToTable(
            "conversation_read_states",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_conversation_read_states_last_read_after_created",
                    "last_read_at IS NULL OR last_read_at >= created_at");

                table.HasCheckConstraint(
                    "ck_conversation_read_states_updated_after_created",
                    "updated_at IS NULL OR updated_at >= created_at");
            });

        builder.HasKey(readState => new { readState.ConversationId, readState.UserProfileId });

        builder.Property(readState => readState.ConversationId)
            .HasColumnName("conversation_id")
            .HasConversion(StronglyTypedIdConverters.ConversationIdConverter)
            .IsRequired();

        builder.Property(readState => readState.UserProfileId)
            .HasColumnName("user_profile_id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .IsRequired();

        builder.Property(readState => readState.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(readState => readState.LastReadAt)
            .HasColumnName("last_read_at");

        builder.Property(readState => readState.IsMarkedUnread)
            .HasColumnName("is_marked_unread")
            .IsRequired();

        builder.Property(readState => readState.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany()
            .HasForeignKey(readState => readState.UserProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(readState => new { readState.UserProfileId, readState.IsMarkedUnread })
            .HasDatabaseName("ix_conversation_read_states_user_unread");
    }
}
