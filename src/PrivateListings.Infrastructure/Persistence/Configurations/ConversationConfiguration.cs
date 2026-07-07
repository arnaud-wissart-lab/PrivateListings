using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Domain.Messaging;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable(
            "conversations",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_conversations_have_two_participants",
                    "cardinality(participant_ids) >= 2");

                table.HasCheckConstraint(
                    "ck_conversations_closed_after_created",
                    "closed_at IS NULL OR closed_at >= created_at");
            });

        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation => conversation.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.ConversationIdConverter)
            .ValueGeneratedNever();

        builder.Property(conversation => conversation.ListingId)
            .HasColumnName("listing_id")
            .HasConversion(StronglyTypedIdConverters.NullableListingIdConverter);

        var participantIds = builder.Property<List<Domain.Common.UserProfileId>>("_participantIds")
            .HasColumnName("participant_ids")
            .HasColumnType("uuid[]")
            .HasConversion(StronglyTypedIdConverters.UserProfileIdListConverter)
            .IsRequired();

        participantIds.Metadata.SetValueComparer(StronglyTypedIdConverters.UserProfileIdListComparer);

        builder.Property(conversation => conversation.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(conversation => conversation.ClosedAt)
            .HasColumnName("closed_at");

        builder.HasOne<Domain.Listings.Listing>()
            .WithMany()
            .HasForeignKey(conversation => conversation.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(conversation => conversation.Messages)
            .WithOne()
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(conversation => conversation.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(conversation => conversation.ListingId)
            .HasDatabaseName("ix_conversations_listing_id");

        builder.HasIndex(conversation => conversation.CreatedAt)
            .HasDatabaseName("ix_conversations_created_at");

        builder.HasIndex("_participantIds")
            .HasDatabaseName("ix_conversations_participant_ids")
            .HasMethod("gin");
    }
}
