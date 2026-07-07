using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Domain.Messaging;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable(
            "messages",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_messages_deleted_after_sent",
                    "deleted_at IS NULL OR deleted_at >= sent_at");
            });

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.MessageIdConverter)
            .ValueGeneratedNever();

        builder.Property(message => message.ConversationId)
            .HasColumnName("conversation_id")
            .HasConversion(StronglyTypedIdConverters.ConversationIdConverter)
            .IsRequired();

        builder.Property(message => message.SenderId)
            .HasColumnName("sender_id")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdConverter)
            .IsRequired();

        builder.Property(message => message.Body)
            .HasColumnName("body")
            .HasMaxLength(4_000)
            .IsRequired();

        builder.Property(message => message.SentAt)
            .HasColumnName("sent_at")
            .IsRequired();

        builder.Property(message => message.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasOne<Domain.Users.UserProfile>()
            .WithMany()
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(message => new { message.ConversationId, message.SentAt })
            .HasDatabaseName("ix_messages_conversation_sent_at");

        builder.HasIndex(message => new { message.SenderId, message.SentAt })
            .HasDatabaseName("ix_messages_sender_sent_at");
    }
}
