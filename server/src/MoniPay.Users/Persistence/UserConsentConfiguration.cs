using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoniPay.Kernel;
using MoniPay.Users.Domain;

namespace MoniPay.Users.Persistence;

/// <summary>Maps <see cref="UserConsent"/> onto the <c>user_consents</c> table.</summary>
internal sealed class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(UsersSchema.UserConsentsTable);

        builder.HasKey(consent => new { consent.UserId, consent.DocumentKind })
            .HasName(UsersSchema.UserConsentsPrimaryKey);

        builder.Property(consent => consent.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(consent => consent.DocumentKind)
            .HasColumnName("document_kind")
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(consent => consent.DocumentVersion)
            .HasColumnName("document_version")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(consent => consent.AcceptedAt).HasColumnName("accepted_at").IsRequired();
    }
}
