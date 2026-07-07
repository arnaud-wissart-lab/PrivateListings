using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateListings.Infrastructure.Identity;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal sealed class AccountSecurityTokenConfiguration : IEntityTypeConfiguration<AccountSecurityToken>
{
    public void Configure(EntityTypeBuilder<AccountSecurityToken> builder)
    {
        builder.ToTable("account_security_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(token => token.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(token => token.Purpose)
            .HasColumnName("purpose")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(token => token.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(token => token.ConsumedAt)
            .HasColumnName("consumed_at");

        builder.HasOne<PrivateListingsUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => new { token.UserId, token.Purpose, token.TokenHash })
            .IsUnique()
            .HasDatabaseName("ux_account_security_tokens_user_purpose_hash");

        builder.HasIndex(token => new { token.UserId, token.Purpose, token.ConsumedAt })
            .HasDatabaseName("ix_account_security_tokens_user_purpose_consumed");

        builder.HasIndex(token => token.ExpiresAt)
            .HasDatabaseName("ix_account_security_tokens_expires_at");
    }
}
