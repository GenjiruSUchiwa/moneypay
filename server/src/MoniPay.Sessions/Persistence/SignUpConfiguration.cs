using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Persistence;

internal sealed class SignUpConfiguration : IEntityTypeConfiguration<SignUp>
{
    public void Configure(EntityTypeBuilder<SignUp> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(SessionsSchema.SignUpsTable);

        builder.HasKey(signUp => signUp.Id).HasName(SessionsSchema.SignUpsPrimaryKey);
        builder.Property(signUp => signUp.Id).HasColumnName("id");
        builder.Property(signUp => signUp.PhoneCiphertext).HasColumnName("phone_ciphertext").IsRequired();
        builder.Property(signUp => signUp.PhoneLookupHash).HasColumnName("phone_lookup_hash").IsRequired();
        builder.Property(signUp => signUp.Locale).HasColumnName("locale").IsRequired();
        builder.Property(signUp => signUp.CodeDigest).HasColumnName("code_digest");
        builder.Property(signUp => signUp.CodeExpiresAt).HasColumnName("code_expires_at");
        builder.Property(signUp => signUp.SignUpTokenDigest).HasColumnName("signup_token_digest");
        builder.Property(signUp => signUp.RegistrationTokenDigest).HasColumnName("registration_token_digest");
        builder.Property(signUp => signUp.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(signUp => signUp.FailedAttempts).HasColumnName("failed_attempts").IsRequired();
        builder.Property(signUp => signUp.ResendCount).HasColumnName("resend_count").IsRequired();
        builder.Property(signUp => signUp.CanResendAt).HasColumnName("can_resend_at").IsRequired();
        builder.Property(signUp => signUp.LockedUntil).HasColumnName("locked_until");
        builder.Property(signUp => signUp.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(signUp => signUp.TermsVersion).HasColumnName("terms_version").HasMaxLength(64).IsRequired();
        builder.Property(signUp => signUp.PrivacyVersion).HasColumnName("privacy_version").HasMaxLength(64).IsRequired();
        builder.Property(signUp => signUp.ProvisionedUserId).HasColumnName("user_id");
        builder.Property(signUp => signUp.BootstrapSessionId).HasColumnName("bootstrap_session_id");
        builder.Property(signUp => signUp.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(signUp => signUp.VerifiedAt).HasColumnName("verified_at");
        builder.Property(signUp => signUp.CompletedAt).HasColumnName("completed_at");

        builder.Property(signUp => signUp.Version).HasColumnName("version").IsConcurrencyToken();

        builder.HasIndex(signUp => signUp.PhoneLookupHash)
            .IsUnique()
            .HasDatabaseName(SessionsSchema.SignUpPhoneActiveWorkflowUnique)
            .HasFilter(SessionsSchema.ActiveStatusFilter);

        builder.HasIndex(signUp => new { signUp.Status, signUp.ExpiresAt })
            .HasDatabaseName(SessionsSchema.SignUpStatusExpiryIndex);

        builder.HasIndex(signUp => signUp.SignUpTokenDigest)
            .IsUnique()
            .HasDatabaseName(SessionsSchema.SignUpTokenDigestUnique)
            .HasFilter("signup_token_digest IS NOT NULL");

        builder.HasIndex(signUp => signUp.RegistrationTokenDigest)
            .IsUnique()
            .HasDatabaseName(SessionsSchema.SignUpRegistrationTokenDigestUnique)
            .HasFilter("registration_token_digest IS NOT NULL");
    }
}
