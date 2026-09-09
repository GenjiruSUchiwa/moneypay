using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Tests.Support;
using MoniPay.Users.Features.Registration;
using MoniPay.Users.Persistence;
using MoniPay.Users.Security;
using Xunit;

namespace MoniPay.Tests.Users.Registration;

public sealed class PhoneRegistrationLookupTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static int sequence;

    [Fact]
    public async Task A_registered_phone_finds_its_user()
    {
        await InRolledBackTransactionAsync(async (register, lookup) =>
        {
            PhoneNumber phone = new(TestPhones.Next());
            RegisteredUser registered = await register.HandleAsync(Command(phone.Value), Cancellation);

            Assert.True(registered.Created);
            Assert.Equal(registered.Id, await lookup.FindUserIdAsync(phone, Cancellation));
            Assert.Null(await lookup.FindUserIdAsync(new PhoneNumber(TestPhones.Next()), Cancellation));
        });
    }

    [Fact]
    public async Task The_two_modules_hash_one_phone_differently()
    {
        PhoneNumber phone = new(TestPhones.Next());
        await Api.RegisterUserAsync(phone);
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        LookupHash forUsers = Api.Services.GetRequiredService<UserLookupDigest>().Compute(phone.Value);

        try
        {
            LookupHash stored = await Api.InScopeAsync<MoniPayDbContext, LookupHash>((database, cancellationToken) =>
                database.Users
                    .Where(user => user.Phone.Hash.Equals(forUsers))
                    .Select(user => user.Phone.Hash)
                    .SingleAsync(cancellationToken));
            LookupHash forSessions = (await Api.ReadSignUpRowAsync(started.SignUpId)).PhoneLookupHash;

            Assert.NotEqual(stored.Value, forSessions.Value);
        }
        finally
        {
            await Api.QueryAsync("TRUNCATE TABLE user_consents, users;", reader => 0);
        }
    }

    [Fact]
    public async Task The_lookup_honors_a_canceled_token()
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        PhoneRegistrationLookup lookup = scope.ServiceProvider.GetRequiredService<PhoneRegistrationLookup>();
        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => lookup.FindUserIdAsync(new PhoneNumber(TestPhones.Next()), canceled.Token));
    }

    private static RegisterUserCommand Command(string phone)
    {
        int unique = Interlocked.Increment(ref sequence);
        return new RegisterUserCommand(
            SignUpId.New(),
            UserId.New(),
            new PhoneNumber(phone),
            new PersonName("Marie"),
            new PersonName("Ngo Nyobé"),
            new EmailAddress($"marie.ngo{unique}@example.com"),
            Locale.FrenchCameroon,
            SignUpFlow.TermsVersion,
            SignUpFlow.PrivacyVersion,
            new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero));
    }

    private async Task InRolledBackTransactionAsync(Func<RegisterUserHandler, PhoneRegistrationLookup, Task> test)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        await using IDbContextTransaction transaction =
            await database.Database.BeginTransactionAsync(Cancellation);

        await test(
            scope.ServiceProvider.GetRequiredService<RegisterUserHandler>(),
            scope.ServiceProvider.GetRequiredService<PhoneRegistrationLookup>());
    }
}
