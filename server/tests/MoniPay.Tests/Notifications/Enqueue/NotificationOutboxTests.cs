using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Notifications;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Tests.Support;
using Npgsql;
using Xunit;

namespace MoniPay.Tests.Notifications.Enqueue;

/// <summary>
/// The outbox contract: enqueue is part of the producer's transaction, the commit signal comes
/// after the commit and only once, the idempotency key admits one delivery, the status read
/// answers with the newest row, and the ciphertext columns hold no plaintext.
/// </summary>
public sealed class NotificationOutboxTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task A_row_enqueued_in_an_open_transaction_is_invisible_to_another_scope_until_commit()
    {
        string idempotencyKey = $"visibility-{Guid.CreateVersion7()}";

        using IServiceScope producerScope = Api.Services.CreateScope();
        MoniPayDbContext database = producerScope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        NotificationOutbox outbox = producerScope.ServiceProvider.GetRequiredService<NotificationOutbox>();

        await using IDbContextTransaction transaction = await database.Database.BeginTransactionAsync(Cancellation);
        outbox.Enqueue(Message(idempotencyKey));
        await database.SaveChangesAsync(Cancellation);

        using IServiceScope otherScope = Api.Services.CreateScope();
        MoniPayDbContext other = otherScope.ServiceProvider.GetRequiredService<MoniPayDbContext>();

        Assert.Equal(0, await other.Notifications.CountAsync(row => row.IdempotencyKey == idempotencyKey, Cancellation));

        await transaction.CommitAsync(Cancellation);

        Assert.Equal(1, await other.Notifications.CountAsync(row => row.IdempotencyKey == idempotencyKey, Cancellation));
    }

    [Fact]
    public async Task The_commit_signal_is_raised_once_after_the_commit_not_before()
    {
        using IServiceScope scope = Api.Services.CreateScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        NotificationOutbox outbox = scope.ServiceProvider.GetRequiredService<NotificationOutbox>();
        DeliverySignal signal = scope.ServiceProvider.GetRequiredService<DeliverySignal>();

        await DrainAsync(signal);

        await using IDbContextTransaction transaction = await database.Database.BeginTransactionAsync(Cancellation);
        outbox.Enqueue(Message($"signal-{Guid.CreateVersion7()}"));
        await database.SaveChangesAsync(Cancellation);

        Assert.False(await signal.WaitAsync(TimeSpan.FromMilliseconds(200), Cancellation));

        await transaction.CommitAsync(Cancellation);

        Assert.True(await signal.WaitAsync(TimeSpan.FromSeconds(1), Cancellation));
        Assert.False(await signal.WaitAsync(TimeSpan.FromMilliseconds(200), Cancellation));
    }

    [Fact]
    public async Task A_duplicate_idempotency_key_fails_on_save()
    {
        string idempotencyKey = $"duplicate-{Guid.CreateVersion7()}";

        using IServiceScope scope = Api.Services.CreateScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        NotificationOutbox outbox = scope.ServiceProvider.GetRequiredService<NotificationOutbox>();

        outbox.Enqueue(Message(idempotencyKey));
        outbox.Enqueue(Message(idempotencyKey));

        DbUpdateException failure = await Assert.ThrowsAsync<DbUpdateException>(
            () => database.SaveChangesAsync(Cancellation));

        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            Assert.IsType<PostgresException>(failure.InnerException).SqlState);
    }

    [Fact]
    public async Task FindLatestStatusAsync_returns_the_newest_row_for_a_correlation_and_kind()
    {
        Guid correlationId = Guid.CreateVersion7();
        const string kind = "VerificationCode";
        string olderKey = $"status-older-{correlationId}";
        string newerKey = $"status-newer-{correlationId}";

        using IServiceScope scope = Api.Services.CreateScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        NotificationOutbox outbox = scope.ServiceProvider.GetRequiredService<NotificationOutbox>();

        outbox.Enqueue(Message(olderKey, correlationId));
        await database.SaveChangesAsync(Cancellation);

        Api.Time.Advance(TimeSpan.FromMinutes(1));

        outbox.Enqueue(Message(newerKey, correlationId));
        await database.SaveChangesAsync(Cancellation);

        await database.Notifications
            .Where(row => row.IdempotencyKey == olderKey)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, NotificationStatus.Sent), Cancellation);

        Assert.Equal(NotificationStatus.Pending, await outbox.FindLatestStatusAsync(correlationId, kind, Cancellation));
        Assert.Null(await outbox.FindLatestStatusAsync(Guid.CreateVersion7(), kind, Cancellation));
    }

    [Fact]
    public async Task The_ciphertext_columns_hold_no_plaintext()
    {
        Guid correlationId = Guid.CreateVersion7();
        const string phone = "+237670123456";
        const string body = "Your MoniPay code is 482913, valid 5 minutes.";
        const string email = "account@proton.me";
        const string subject = "Welcome to MoniPay";

        using IServiceScope scope = Api.Services.CreateScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        NotificationOutbox outbox = scope.ServiceProvider.GetRequiredService<NotificationOutbox>();

        outbox.Enqueue(Message($"cipher-sms-{correlationId}", correlationId) with
        {
            Recipient = phone,
            Body = body,
        });
        outbox.Enqueue(Message($"cipher-email-{correlationId}", correlationId) with
        {
            Channel = NotificationChannel.Email,
            Recipient = email,
            Subject = subject,
            Body = "Welcome.",
        });
        await database.SaveChangesAsync(Cancellation);

        (string recipient, string? subjectCiphertext, string? bodyCiphertext, string hint) sms =
            await RowOfAsync($"cipher-sms-{correlationId}");
        (string recipient, string? subjectCiphertext, string? bodyCiphertext, string hint) welcome =
            await RowOfAsync($"cipher-email-{correlationId}");

        Assert.DoesNotContain(phone, sms.recipient, StringComparison.Ordinal);
        Assert.DoesNotContain("482913", sms.bodyCiphertext, StringComparison.Ordinal);
        Assert.Null(sms.subjectCiphertext);
        Assert.Equal("3456", sms.hint);

        Assert.DoesNotContain(email, welcome.recipient, StringComparison.Ordinal);
        Assert.DoesNotContain(subject, welcome.subjectCiphertext, StringComparison.Ordinal);
        Assert.Equal("proton.m", welcome.hint);
    }

    [Fact]
    public void A_message_outside_the_contract_bounds_is_refused_at_the_call_site_not_at_save()
    {
        using IServiceScope scope = Api.Services.CreateScope();
        NotificationOutbox outbox = scope.ServiceProvider.GetRequiredService<NotificationOutbox>();

        OutboundMessage undefinedChannel = Message("bounds-channel") with { Channel = (NotificationChannel)99 };
        OutboundMessage longKind = Message("bounds-kind") with { Kind = new string('k', NotificationsSchema.KindMaxLength + 1) };
        OutboundMessage noCorrelation = Message("bounds-correlation") with { CorrelationId = default };

        Assert.Throws<ArgumentOutOfRangeException>(() => outbox.Enqueue(undefinedChannel));
        Assert.Throws<ArgumentOutOfRangeException>(() => outbox.Enqueue(longKind));
        Assert.Throws<ArgumentException>(() => outbox.Enqueue(noCorrelation));
    }

    private static OutboundMessage Message(string idempotencyKey, Guid? correlationId = null) => new(
        Channel: NotificationChannel.Sms,
        Recipient: "+237670123456",
        Subject: null,
        Body: "Your MoniPay code is 482913, valid 5 minutes.",
        Kind: "VerificationCode",
        Required: true,
        IdempotencyKey: idempotencyKey,
        ExpiresAt: null,
        CorrelationId: correlationId ?? Guid.CreateVersion7());

    private async Task DrainAsync(DeliverySignal signal)
    {
        while (await signal.WaitAsync(TimeSpan.Zero, Cancellation))
        {
        }
    }

    private async Task<(string Recipient, string? Subject, string? Body, string Hint)> RowOfAsync(string idempotencyKey)
    {
        IReadOnlyList<(string Recipient, string? Subject, string? Body, string Hint)> rows = await Api.QueryAsync(
            """
            SELECT recipient_ciphertext, subject_ciphertext, body_ciphertext, recipient_hint
            FROM notifications
            WHERE idempotency_key = @key;
            """,
            reader => (
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3)),
            ("key", idempotencyKey));

        return Assert.Single(rows);
    }
}
