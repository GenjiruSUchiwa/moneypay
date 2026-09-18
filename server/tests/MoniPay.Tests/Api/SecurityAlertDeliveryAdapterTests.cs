using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Notifications;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Notifications.Security;
using MoniPay.Persistence;
using MoniPay.Sessions.Providers;
using MoniPay.Tests.Support;
using MoniPay.Users.Features.Registration;
using Xunit;

namespace MoniPay.Tests.Api;

public sealed class SecurityAlertDeliveryAdapterTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Theory]
    [InlineData(Locale.EnglishTag, "For your security")]
    [InlineData(Locale.FrenchTag, "Pour votre sécurité")]
    public async Task It_renders_the_reuse_alert_once_per_channel_in_the_stored_locale(
        string localeTag,
        string expectedPhrase)
    {
        PhoneNumber phone = new(TestPhones.Next());
        EmailAddress email = new($"alert{Guid.NewGuid():N}@example.com");
        RegisteredUser user = await RegisterAsync(phone, email, new Locale(localeTag));

        try
        {
            SecurityAlert alert = new(
                user.Id,
                SecurityAlertKind.RefreshTokenReuseDetected,
                new DateTimeOffset(2026, 9, 18, 10, 30, 0, TimeSpan.Zero),
                $"refresh-reuse:{Guid.NewGuid()}");

            await EnqueueAndSaveAsync(alert);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            List<Notification> rows = await database.Notifications
                .AsNoTracking()
                .Where(row => row.IdempotencyKey == $"{alert.IdempotencyKey}:sms"
                    || row.IdempotencyKey == $"{alert.IdempotencyKey}:email")
                .ToListAsync(Cancellation);

            Assert.Equal(2, rows.Count);
            Notification sms = Assert.Single(rows, row => row.Channel == NotificationChannel.Sms);
            Notification mail = Assert.Single(rows, row => row.Channel == NotificationChannel.Email);
            Assert.True(sms.Required);
            Assert.True(mail.Required);
            Assert.Null(sms.ExpiresAt);
            Assert.Null(mail.ExpiresAt);
            Assert.Equal(user.Id.Value, sms.CorrelationId);
            Assert.Equal(user.Id.Value, mail.CorrelationId);

            RecipientProtector protector = Api.Services.GetRequiredService<RecipientProtector>();
            string smsBody = protector.Unprotect(Assert.IsType<Ciphertext>(sms.BodyCiphertext));
            string subject = protector.Unprotect(Assert.IsType<Ciphertext>(mail.SubjectCiphertext));
            string mailBody = protector.Unprotect(Assert.IsType<Ciphertext>(mail.BodyCiphertext));

            Assert.Contains("MoniPay", smsBody, StringComparison.Ordinal);
            Assert.Contains("2026", smsBody, StringComparison.Ordinal);
            Assert.Contains("11:30", smsBody, StringComparison.Ordinal);
            Assert.Contains("11:30", mailBody, StringComparison.Ordinal);
            Assert.Contains(expectedPhrase, smsBody, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(expectedPhrase, mailBody, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("MoniPay", subject, StringComparison.Ordinal);
            Assert.DoesNotContain(phone.Value, smsBody, StringComparison.Ordinal);
            Assert.DoesNotContain(phone.Value, subject, StringComparison.Ordinal);
            Assert.DoesNotContain(email.Value, mailBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(email.Value, subject, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task An_alert_for_a_user_without_a_contact_enqueues_nothing()
    {
        SecurityAlert alert = new(
            UserId.New(),
            SecurityAlertKind.SessionRevoked,
            new DateTimeOffset(2026, 9, 18, 10, 30, 0, TimeSpan.Zero),
            $"session-revoked:{Guid.NewGuid()}");

        await EnqueueAndSaveAsync(alert);

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Assert.False(await database.Notifications.AnyAsync(
            row => row.CorrelationId == alert.UserId.Value, Cancellation));
    }

    private async Task EnqueueAndSaveAsync(SecurityAlert alert)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        ISecurityAlertSender sender = scope.ServiceProvider.GetRequiredService<ISecurityAlertSender>();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();

        await sender.EnqueueAsync(alert, Cancellation);
        await database.SaveChangesAsync(Cancellation);
    }

    private Task<RegisteredUser> RegisterAsync(PhoneNumber phone, EmailAddress email, Locale locale)
    {
        RegisterUserCommand command = new(
            SignUpId.New(),
            UserId.New(),
            phone,
            new PersonName("Marie"),
            new PersonName("Ngo"),
            email,
            locale,
            SignUpFlow.TermsVersion,
            SignUpFlow.PrivacyVersion,
            new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero));

        return Api.InScopeAsync<RegisterUserHandler, RegisteredUser>(
            (handler, cancellationToken) => handler.HandleAsync(command, cancellationToken));
    }
}
