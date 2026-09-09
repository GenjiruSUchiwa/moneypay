using MoniPay.Kernel;

namespace MoniPay.Notifications.Domain;

internal sealed class Notification
{
    private Notification()
    {
    }

    public Guid Id { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public string Kind { get; private set; } = string.Empty;

    public Ciphertext RecipientCiphertext { get; private set; }

    public string RecipientHint { get; private set; } = string.Empty;

    public Ciphertext? SubjectCiphertext { get; private set; }

    public Ciphertext? BodyCiphertext { get; private set; }

    public bool Required { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public Guid CorrelationId { get; private set; }

    public NotificationStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public DateTimeOffset? LeaseUntil { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public string? ProviderReference { get; private set; }

    public string? LastErrorCode { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public static Notification Pending(
        OutboundMessage message,
        Ciphertext recipientCiphertext,
        string recipientHint,
        Ciphertext? subjectCiphertext,
        Ciphertext? bodyCiphertext,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(recipientHint);
        ArgumentNullException.ThrowIfNull(bodyCiphertext);

        return new Notification
        {
            Id = Guid.CreateVersion7(),
            Channel = message.Channel,
            Kind = message.Kind,
            RecipientCiphertext = recipientCiphertext,
            RecipientHint = recipientHint,
            SubjectCiphertext = subjectCiphertext,
            BodyCiphertext = bodyCiphertext,
            Required = message.Required,
            IdempotencyKey = message.IdempotencyKey,
            CorrelationId = message.CorrelationId,
            Status = NotificationStatus.Pending,
            Attempts = 0,
            NextAttemptAt = now,
            ExpiresAt = message.ExpiresAt,
            CreatedAt = now,
        };
    }

    public void MarkSent(string providerReference, DateTimeOffset now)
    {
        Attempts += 1;
        Status = NotificationStatus.Sent;
        SentAt = now;
        ProviderReference = providerReference;
        BodyCiphertext = null;
        LeaseUntil = null;
    }

    public bool RecordRetry(string code, DateTimeOffset now)
    {
        if (RetrySchedule.NextDelay(Kind, Attempts + 1) is not { } delay)
        {
            Fail(code);
            return false;
        }

        Attempts += 1;
        LastErrorCode = code;
        LeaseUntil = null;
        NextAttemptAt = now + delay;
        return true;
    }

    public void Fail(string code)
    {
        Attempts += 1;
        Status = NotificationStatus.Failed;
        LastErrorCode = code;
        LeaseUntil = null;
    }

    public void Expire()
    {
        Status = NotificationStatus.Expired;
        LeaseUntil = null;
    }

    public void Skip(string code)
    {
        LastErrorCode = code;
        LeaseUntil = null;
    }
}
