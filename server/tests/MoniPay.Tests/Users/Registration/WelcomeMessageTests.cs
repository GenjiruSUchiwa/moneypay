using System.Globalization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MoniPay.Api.Composition;
using MoniPay.Kernel;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Tests.Fakes;
using MoniPay.Tests.Support;
using MoniPay.Users.Features.Registration;
using MoniPay.Users.Persistence;
using MoniPay.Users.Providers;
using Xunit;

namespace MoniPay.Tests.Users.Registration;

/// <summary>
/// The Users side of the welcome email: the renderer picks the stored locale and preserves the
/// validated name, the handler stages one best-effort message, and a failure or a cancellation
/// never leaks personal data or turns cancellation into a delivery failure.
/// </summary>
public sealed class WelcomeMessageTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static int sequence;

    [Fact]
    public void The_renderer_uses_the_stored_locale_and_preserves_the_validated_name()
    {
        WelcomeMessageRenderer renderer = Api.Services.GetRequiredService<WelcomeMessageRenderer>();
        UserId userId = UserId.New();
        EmailAddress recipient = new("marie.ngo@example.com");
        PersonName name = new("Ngo Nyobé");

        WelcomeMessage french = renderer.Render(userId, recipient, name, Locale.French);
        WelcomeMessage cameroon = renderer.Render(userId, recipient, name, Locale.FrenchCameroon);
        WelcomeMessage english = renderer.Render(userId, recipient, name, Locale.English);

        Assert.Equal("Bienvenue sur MoniPay", french.Subject);
        Assert.Equal("Bonjour Ngo Nyobé, votre compte MoniPay est prêt.", french.Body);
        Assert.Equal(french.Subject, cameroon.Subject);
        Assert.Equal(french.Body, cameroon.Body);
        Assert.Equal("Welcome to MoniPay", english.Subject);
        Assert.Equal("Hi Ngo Nyobé, your MoniPay account is ready.", english.Body);
        Assert.Equal(recipient, french.Recipient);
        Assert.Equal($"welcome:{userId}", french.IdempotencyKey);
    }

    [Fact]
    public Task Rendering_restores_the_ambient_culture()
    {
        WelcomeMessageRenderer renderer = Api.Services.GetRequiredService<WelcomeMessageRenderer>();
        CultureInfo beforeCulture = CultureInfo.CurrentCulture;
        CultureInfo beforeUi = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            renderer.Render(UserId.New(), new EmailAddress("marie@example.com"), new PersonName("Marie"), Locale.French);

            Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
            Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.CurrentCulture = beforeCulture;
            CultureInfo.CurrentUICulture = beforeUi;
        }

        return Task.CompletedTask;
    }

    [Fact]
    public void The_message_hides_everything_it_carries()
    {
        WelcomeMessage message = new(
            UserId.New(),
            new EmailAddress("marie.ngo@example.com"),
            "Bienvenue sur MoniPay",
            "Bonjour Marie, votre compte MoniPay est prêt.",
            "welcome:00000000-0000-0000-0000-000000000000");

        Assert.Equal(nameof(WelcomeMessage), message.ToString());
        Assert.DoesNotContain("marie", message.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Marie", message.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_enqueue_is_best_effort_and_warns_once_without_plaintext()
    {
        RecordingLoggerProvider logs = new();
        using WebApplicationFactory<Program> host = HostWithSender(new FailingWelcomeSender(), logs);
        RegisterUserCommand command = Command();

        try
        {
            RegisteredUser registered = await RegisterAsync(host, command, Cancellation);

            Assert.True(registered.Created);
            Assert.Equal(0, await WelcomeCountAsync(registered.Id));

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(1, await database.Users.CountAsync(user => user.Id == registered.Id, Cancellation));
            Assert.Equal(2, await database.UserConsents.CountAsync(
                consent => consent.UserId == registered.Id, Cancellation));

            RecordingLoggerProvider.LogEntry warning = Assert.Single(
                logs.Entries,
                entry => entry.Level == LogLevel.Warning && entry.Category == typeof(RegisterUserHandler).FullName);
            Assert.Contains(command.UserId.ToString(), warning.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(command.Email.Value, warning.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(command.FirstName.Value, warning.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(command.Email.Value, warning.Exception?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(command.FirstName.Value, warning.Exception?.ToString() ?? string.Empty, StringComparison.Ordinal);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_caller_cancellation_propagates_without_an_optional_email_warning()
    {
        RecordingLoggerProvider logs = new();
        using CancellationTokenSource cancellation = new();
        using WebApplicationFactory<Program> host = HostWithSender(new CancellingWelcomeSender(cancellation), logs);
        RegisterUserCommand command = Command();

        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scope.ServiceProvider.GetRequiredService<RegisterUserHandler>().HandleAsync(command, cancellation.Token));

        Assert.DoesNotContain(
            logs.Entries,
            entry => entry.Level == LogLevel.Warning && entry.Category == typeof(RegisterUserHandler).FullName);
        Assert.Equal(0, await WelcomeCountAsync(command.UserId));
    }

    [Fact]
    public async Task The_sender_receives_the_callers_cancellation_token_and_the_rendered_message()
    {
        RecordingWelcomeSender port = new();
        using WebApplicationFactory<Program> host = HostWithSender(port, new RecordingLoggerProvider());
        RegisterUserCommand command = Command();
        using CancellationTokenSource cancellation = new();

        try
        {
            RegisteredUser registered = await RegisterAsync(host, command, cancellation.Token);
            WelcomeMessage message = Assert.IsType<WelcomeMessage>(port.Message);

            Assert.True(registered.Created);
            Assert.Equal(cancellation.Token, port.Token);
            Assert.Equal(command.Email, message.Recipient);
            Assert.Equal($"welcome:{registered.Id}", message.IdempotencyKey);
            Assert.Contains(command.FirstName.Value, message.Body, StringComparison.Ordinal);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    private WebApplicationFactory<Program> HostWithSender(IWelcomeMessageSender sender, RecordingLoggerProvider logs) =>
        Api.CreateHost(builder =>
        {
            MoniPayApi.UseNotificationTestSettings(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Api.Time);
                services.RemoveAll<IWelcomeMessageSender>();
                services.AddSingleton(sender);
                services.AddLogging(logging => logging.AddProvider(logs));
            });
        });

    private static async Task<RegisteredUser> RegisterAsync(
        WebApplicationFactory<Program> host,
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<RegisterUserHandler>()
            .HandleAsync(command, cancellationToken);
    }

    private async Task<int> WelcomeCountAsync(UserId userId)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        return await database.Notifications.CountAsync(
            row => row.Kind == WelcomeMessageDeliveryAdapter.WelcomeKind && row.CorrelationId == userId.Value,
            Cancellation);
    }

    private static RegisterUserCommand Command()
    {
        int unique = Interlocked.Increment(ref sequence);
        return new RegisterUserCommand(
            SignUpId.New(),
            UserId.New(),
            new PhoneNumber(TestPhones.Next()),
            new PersonName("Marie"),
            new PersonName("Ngo Nyobé"),
            new EmailAddress($"marie.ngo{unique}@example.com"),
            Locale.FrenchCameroon,
            "terms-2026-08",
            "privacy-2026-07",
            new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero));
    }

    private sealed class RecordingWelcomeSender : IWelcomeMessageSender
    {
        public WelcomeMessage? Message { get; private set; }

        public CancellationToken Token { get; private set; }

        public Task EnqueueAsync(WelcomeMessage message, CancellationToken cancellationToken)
        {
            Message = message;
            Token = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class CancellingWelcomeSender(CancellationTokenSource cancellation) : IWelcomeMessageSender
    {
        public async Task EnqueueAsync(WelcomeMessage message, CancellationToken cancellationToken)
        {
            await cancellation.CancelAsync();
            throw new OperationCanceledException(cancellationToken);
        }
    }
}
