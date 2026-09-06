using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Persistence;

/// <summary>Maps <see cref="Session"/> onto the <c>sessions</c> table.</summary>
internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(SessionsSchema.SessionsTable);

        builder.HasKey(session => session.Id).HasName(SessionsSchema.SessionsPrimaryKey);
        builder.Property(session => session.Id).HasColumnName("id");
        builder.Property(session => session.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(session => session.DeviceId).HasColumnName("device_id").IsRequired();
        builder.Property(session => session.TokenFamilyId).HasColumnName("token_family_id").IsRequired();
        builder.Property(session => session.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(session => session.LastSeenAt).HasColumnName("last_seen_at").IsRequired();
        builder.Property(session => session.RevokedAt).HasColumnName("revoked_at");
        builder.Property(session => session.RevokeReason)
            .HasColumnName("revoke_reason")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(session => session.Version).HasColumnName("version").IsConcurrencyToken();

        builder.HasIndex(session => new { session.UserId, session.RevokedAt })
            .HasDatabaseName(SessionsSchema.SessionUserRevokedIndex);

        builder.HasIndex(session => new { session.TokenFamilyId, session.DeviceId })
            .IsUnique()
            .HasDatabaseName(SessionsSchema.SessionFamilyDeviceUnique);
    }
}
