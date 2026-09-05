using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoniPay.Notifications.Domain;

namespace MoniPay.Notifications.Persistence;

/// <summary>Maps <see cref="Notification"/> onto the <c>notifications</c> table.</summary>
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(NotificationsSchema.NotificationsTable);

        builder.HasKey(notification => notification.Id)
            .HasName(NotificationsSchema.NotificationsPrimaryKey);

        builder.Property(notification => notification.Id).HasColumnName("id");
        builder.Property(notification => notification.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(notification => notification.Kind).HasColumnName("kind").HasMaxLength(48).IsRequired();
        builder.Property(notification => notification.RecipientCiphertext)
            .HasColumnName("recipient_ciphertext")
            .IsRequired();
        builder.Property(notification => notification.RecipientHint)
            .HasColumnName("recipient_hint")
            .HasMaxLength(8)
            .IsRequired();
        builder.Property(notification => notification.SubjectCiphertext).HasColumnName("subject_ciphertext");
        builder.Property(notification => notification.BodyCiphertext).HasColumnName("body_ciphertext");
        builder.Property(notification => notification.Required).HasColumnName("required").IsRequired();
        builder.Property(notification => notification.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(notification => notification.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(notification => notification.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(notification => notification.Attempts).HasColumnName("attempts").IsRequired();
        builder.Property(notification => notification.NextAttemptAt).HasColumnName("next_attempt_at").IsRequired();
        builder.Property(notification => notification.LeaseUntil).HasColumnName("lease_until");
        builder.Property(notification => notification.ExpiresAt).HasColumnName("expires_at");
        builder.Property(notification => notification.ProviderReference)
            .HasColumnName("provider_reference")
            .HasMaxLength(128);
        builder.Property(notification => notification.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(64);
        builder.Property(notification => notification.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(notification => notification.SentAt).HasColumnName("sent_at");

        builder.HasIndex(notification => new { notification.Status, notification.NextAttemptAt })
            .HasDatabaseName(NotificationsSchema.ClaimIndex);

        builder.HasIndex(notification => notification.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName(NotificationsSchema.IdempotencyKeyUnique);

        builder.HasIndex(notification => notification.CorrelationId)
            .HasDatabaseName(NotificationsSchema.CorrelationIdIndex);
    }
}
