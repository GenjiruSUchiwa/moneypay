using MoniPay.Kernel;

namespace MoniPay.Notifications.Domain;

/// <summary>
/// One outbound message on its way out, as the <c>notifications</c> table holds it. Everything
/// that could identify or read the recipient — the phone or email, the subject, the body — is
/// ciphertext; only <see cref="RecipientHint"/> is ever in the clear.
/// </summary>
internal sealed class Notification
{
    private Notification()
    {
    }

    public Guid Id { get; private set; }

    public NotificationChannel Channel { get; private set; }

    /// <summary>A stable message kind, such as <c>VerificationCode</c>.</summary>
    public string Kind { get; private set; } = string.Empty;

    public Ciphertext RecipientCiphertext { get; private set; }

    /// <summary>The last four digits of a phone, or the email domain — for support, never for code.</summary>
    public string RecipientHint { get; private set; } = string.Empty;

    public Ciphertext? SubjectCiphertext { get; private set; }

    /// <summary>Cleared the moment the provider accepts the message.</summary>
    public Ciphertext? BodyCiphertext { get; private set; }

    /// <summary>Whether exhausted retries raise an alert.</summary>
    public bool Required { get; private set; }

    /// <summary>One delivery per business event; unique on the table.</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>The sign-up, user, or session identifier the message belongs to.</summary>
    public Guid CorrelationId { get; private set; }

    public NotificationStatus Status { get; private set; }

    public int Attempts { get; private set; }

    /// <summary>The earliest time the worker may claim the row.</summary>
    public DateTimeOffset NextAttemptAt { get; private set; }

    /// <summary>Until when a worker holds the claim; expires when a worker crashes.</summary>
    public DateTimeOffset? LeaseUntil { get; private set; }

    /// <summary>After this the message is useless and is marked Expired instead of sent.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    public string? ProviderReference { get; private set; }

    public string? LastErrorCode { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    /// <summary>
    /// Opens the row in the <see cref="NotificationStatus.Pending"/> state, eligible for the
    /// next claim. The worker, not this factory, decides anything after this point.
    /// </summary>
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

    /// <summary>
    /// The provider took it: the attempt is counted and the body is cleared at once so a later
    /// read reveals no code.
    /// </summary>
    public void MarkSent(string providerReference, DateTimeOffset now)
    {
        Attempts += 1;
        Status = NotificationStatus.Sent;
        SentAt = now;
        ProviderReference = providerReference;
        BodyCiphertext = null;
        LeaseUntil = null;
    }

    /// <summary>
    /// Counts the attempt and books the next one from the kind's <see cref="RetrySchedule"/>.
    /// Returns <c>false</c> when the schedule is exhausted and the row has ended Failed.
    /// </summary>
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

    /// <summary>A permanent refusal: the attempt is counted and no further one is booked.</summary>
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

    /// <summary>Leaves the row Pending and eligible: nothing can send it yet, and nothing tried.</summary>
    public void Skip(string code)
    {
        LastErrorCode = code;
        LeaseUntil = null;
    }
}
