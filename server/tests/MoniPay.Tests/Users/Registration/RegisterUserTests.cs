using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Api.Composition;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Notifications;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Notifications.Security;
using MoniPay.Persistence;
using MoniPay.Tests.Support;
using MoniPay.Users.Features.Registration;
using MoniPay.Users.Persistence;
using Xunit;

namespace MoniPay.Tests.Users.Registration;

public sealed class RegisterUserTests(MoniPayApi api)
{
    private static int sequence;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_same_sign_up_returns_the_same_user_without_a_second_row()
    {
        await InRolledBackTransactionAsync(async (handler, database) =>
        {
            RegisterUserCommand command = Command();

            RegisteredUser first = await handler.HandleAsync(command, Cancellation);
            RegisteredUser replay = await handler.HandleAsync(command with { UserId = UserId.New() }, Cancellation);

            Assert.True(first.Created);
            Assert.Equal(command.UserId, first.Id);
            Assert.False(replay.Created);
            Assert.Equal(first.Id, replay.Id);
            Assert.Equal(1, await database.Users.CountAsync(user => user.SignUpId == command.SignUpId, Cancellation));
            Assert.Equal(2, await database.UserConsents.CountAsync(consent => consent.UserId == first.Id, Cancellation));
        });
    }

    [Fact]
    public async Task A_first_registration_stages_one_welcome_for_the_normalized_recipient()
    {
        await InRolledBackTransactionAsync(async (handler, database) =>
        {
            RegisterUserCommand command = Command(email: "Marie.Ngo@Example.com");

            RegisteredUser registered = await handler.HandleAsync(command, Cancellation);

            RecipientProtector protector = api.Services.GetRequiredService<RecipientProtector>();
            Notification welcome = await database.Notifications.SingleAsync(
                row => row.Kind == WelcomeMessageDeliveryAdapter.WelcomeKind && row.CorrelationId == registered.Id.Value,
                Cancellation);
            Assert.Equal(NotificationChannel.Email, welcome.Channel);
            Assert.False(welcome.Required);
            Assert.Null(welcome.ExpiresAt);
            Assert.Equal(NotificationStatus.Pending, welcome.Status);
            Assert.Equal($"welcome:{registered.Id}", welcome.IdempotencyKey);
            Assert.DoesNotContain(command.Email.Value, welcome.IdempotencyKey, StringComparison.Ordinal);
            Assert.DoesNotContain(command.FirstName.Value, welcome.IdempotencyKey, StringComparison.Ordinal);
            Assert.Equal(command.Email.Value, protector.Unprotect(welcome.RecipientCiphertext));
        });
    }

    [Fact]
    public async Task A_replay_returns_the_user_without_a_second_welcome()
    {
        await InRolledBackTransactionAsync(async (handler, database) =>
        {
            RegisterUserCommand command = Command();

            RegisteredUser first = await handler.HandleAsync(command, Cancellation);
            RegisteredUser replay = await handler.HandleAsync(command with { UserId = UserId.New() }, Cancellation);

            Assert.True(first.Created);
            Assert.False(replay.Created);
            Assert.Equal(1, await database.Notifications.CountAsync(
                row => row.Kind == WelcomeMessageDeliveryAdapter.WelcomeKind && row.CorrelationId == first.Id.Value,
                Cancellation));
        });
    }

    [Fact]
    public async Task A_contact_conflict_leaves_no_welcome_row()
    {
        string email = $"taken.{Guid.NewGuid():N}@example.com";
        await api.RegisterUserAsync(new PhoneNumber(TestPhones.Next()), email);

        try
        {
            await using AsyncServiceScope scope = api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            RegisterUserCommand command = Command(email: email);

            await Assert.ThrowsAsync<RefusalException>(
                () => scope.ServiceProvider.GetRequiredService<RegisterUserHandler>().HandleAsync(command, Cancellation));

            Assert.Equal(0, await database.Notifications.CountAsync(
                row => row.Kind == WelcomeMessageDeliveryAdapter.WelcomeKind && row.CorrelationId == command.UserId.Value,
                Cancellation));
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task A_second_user_with_the_same_verified_phone_is_refused()
    {
        await InRolledBackTransactionAsync(async (handler, _) =>
        {
            RegisterUserCommand first = Command();
            await handler.HandleAsync(first, Cancellation);

            RefusalException refusal = await Assert.ThrowsAsync<RefusalException>(
                () => handler.HandleAsync(Command(phone: first.Phone.Value), Cancellation));

            Assert.Equal(MoniPayErrorTypes.PhoneAlreadyRegistered, refusal.Type);
        });
    }

    [Fact]
    public async Task A_second_user_with_the_same_normalized_email_is_refused()
    {
        await InRolledBackTransactionAsync(async (handler, _) =>
        {
            // Two spellings, one normalized value: the lookup hash folds the case.
            RegisterUserCommand first = Command(email: "Marie.Ngo@Example.com");
            await handler.HandleAsync(first, Cancellation);

            RefusalException refusal = await Assert.ThrowsAsync<RefusalException>(
                () => handler.HandleAsync(Command(email: "MARIE.NGO@example.COM"), Cancellation));

            Assert.Equal(MoniPayErrorTypes.EmailAlreadyRegistered, refusal.Type);
        });
    }

    [Fact]
    public async Task Stored_rows_and_captured_logs_contain_no_plaintext_personal_data()
    {
        RegisterUserCommand command = Command();
        try
        {
            await using AsyncServiceScope scope = api.Services.CreateAsyncScope();

            RegisteredUser registered = await scope.ServiceProvider.GetRequiredService<RegisterUserHandler>()
                .HandleAsync(command, Cancellation);

            Assert.True(registered.Created);
            string[] plaintexts =
            [
                command.FirstName.Value,
                command.LastName.Value,
                command.Phone.Value,
                command.Email.Value,
                command.Email.LookupValue,
            ];
            foreach (string plaintext in plaintexts)
            {
                Assert.Empty(await api.RowsContainingAsync(plaintext));
                Assert.DoesNotContain(
                    api.Logs.Entries,
                    entry => entry.Message.Contains(plaintext, StringComparison.OrdinalIgnoreCase));
            }
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task Two_parallel_registrations_for_one_email_create_one_user()
    {
        // The handler's own read is by sign-up id only, so with two distinct sign-ups nothing
        // but the unique index can decide this race.
        const string email = "race@example.com";
        try
        {
            RegisteredUser?[] outcomes = await Task.WhenAll(
                RegisterCatchingRefusalAsync(Command(email: email)),
                RegisterCatchingRefusalAsync(Command(email: email)));

            Assert.Single(outcomes, outcome => outcome is not null);
            await using AsyncServiceScope scope = api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(1, await database.Users.CountAsync(Cancellation));
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task The_handler_honors_a_canceled_token()
    {
        await using AsyncServiceScope scope = api.Services.CreateAsyncScope();
        RegisterUserHandler handler = scope.ServiceProvider.GetRequiredService<RegisterUserHandler>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(Command(), new CancellationToken(canceled: true)));
    }

    private static RegisterUserCommand Command(string? phone = null, string? email = null)
    {
        int unique = Interlocked.Increment(ref sequence);
        return new RegisterUserCommand(
            SignUpId.New(),
            UserId.New(),
            new PhoneNumber(phone ?? $"2376{50_000_000 + unique:D8}"),
            new PersonName("Marie"),
            new PersonName("Ngo Nyobé"),
            new EmailAddress(email ?? $"marie.ngo{unique}@example.com"),
            Locale.FrenchCameroon,
            "terms-2026-08",
            "privacy-2026-07",
            new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero));
    }

    private async Task InRolledBackTransactionAsync(
        Func<RegisterUserHandler, MoniPayDbContext, Task> test)
    {
        await using AsyncServiceScope scope = api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        await using IDbContextTransaction transaction =
            await database.Database.BeginTransactionAsync(Cancellation);

        // Disposing without a commit rolls back: the shared database stays clean.
        await test(scope.ServiceProvider.GetRequiredService<RegisterUserHandler>(), database);
    }

    private async Task<RegisteredUser?> RegisterCatchingRefusalAsync(RegisterUserCommand command)
    {
        await using AsyncServiceScope scope = api.Services.CreateAsyncScope();
        try
        {
            return await scope.ServiceProvider.GetRequiredService<RegisterUserHandler>()
                .HandleAsync(command, Cancellation);
        }
        catch (RefusalException refusal)
        {
            Assert.Equal(MoniPayErrorTypes.EmailAlreadyRegistered, refusal.Type);
            return null;
        }
    }

    private Task CleanUsersTablesAsync() =>
        api.QueryAsync("TRUNCATE TABLE user_consents, users;", reader => 0);
}
