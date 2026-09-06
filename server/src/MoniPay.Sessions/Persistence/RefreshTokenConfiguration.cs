using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Persistence;

/// <summary>Maps <see cref="RefreshToken"/> onto the <c>refresh_tokens</c> table.</summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(SessionsSchema.RefreshTokensTable);

        builder.HasKey(token => token.Id).HasName(SessionsSchema.RefreshTokensPrimaryKey);
        builder.Property(token => token.Id).HasColumnName("id");
        builder.Property(token => token.SessionId).HasColumnName("session_id");
        builder.Property(token => token.TokenDigest).HasColumnName("token_digest").IsRequired();
        builder.Property(token => token.CreatedAt).HasColumnName("created_at");
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at");
        builder.Property(token => token.UsedAt).HasColumnName("used_at");
        builder.Property(token => token.ReplacedById).HasColumnName("replaced_by_id");

        builder.HasIndex(token => token.TokenDigest)
            .IsUnique()
            .HasDatabaseName(SessionsSchema.RefreshTokenDigestUnique);

        builder.HasIndex(token => token.SessionId)
            .IsUnique()
            .HasDatabaseName(SessionsSchema.RefreshTokenActivePerSessionUnique)
            .HasFilter("used_at IS NULL");

        builder.HasIndex(token => token.ExpiresAt)
            .HasDatabaseName(SessionsSchema.RefreshTokenExpiryIndex);
    }
}
