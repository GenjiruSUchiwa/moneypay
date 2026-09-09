using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoniPay.Users.Domain;

namespace MoniPay.Users.Persistence;

internal sealed class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(UsersSchema.UserConsentsTable);

        builder.HasKey(consent => new { consent.UserId, consent.DocumentKind })
            .HasName(UsersSchema.UserConsentsPrimaryKey);

        builder.Property(consent => consent.UserId).HasColumnName("user_id");
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
