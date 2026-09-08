using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Users.Domain;
using MoniPay.Users.Persistence;
using MoniPay.Users.Ports;
using MoniPay.Users.Security;
using Npgsql;

namespace MoniPay.Users.Features.Registration;

/// <summary>
/// Provisions exactly one user per sign-up: encrypts the contact data, computes the keyed
/// lookup hashes, and writes the user with its two consent rows. Idempotent on the sign-up id.
/// It saves inside the caller's ambient transaction — that is how it observes a
/// unique-constraint violation and maps it — but it opens no transaction of its own and never
/// commits: the ambient transaction decides. The welcome email is staged in the same save and is
/// best effort: a render or enqueue failure is logged and registration continues.
/// </summary>
public sealed class RegisterUserHandler
{
    private readonly MoniPayDbContext database;
    private readonly UserPersonalDataProtector personalData;
    private readonly UserLookupDigest lookupDigest;
    private readonly IWelcomeMessageSender welcomeSender;
    private readonly WelcomeMessageRenderer welcomeRenderer;
    private readonly ILogger<RegisterUserHandler> logger;

    internal RegisterUserHandler(
        MoniPayDbContext database,
        UserPersonalDataProtector personalData,
        UserLookupDigest lookupDigest,
        IWelcomeMessageSender welcomeSender,
        WelcomeMessageRenderer welcomeRenderer,
        ILogger<RegisterUserHandler> logger)
    {
        this.database = database;
        this.personalData = personalData;
        this.lookupDigest = lookupDigest;
        this.welcomeSender = welcomeSender;
        this.welcomeRenderer = welcomeRenderer;
        this.logger = logger;
    }

    /// <summary>Registers the user, or returns the one this sign-up already provisioned.</summary>
    /// <exception cref="RefusalException">
    /// <c>phone-already-registered</c> or <c>email-already-registered</c> when another user
    /// holds the contact — the unique index decides, not a preliminary read.
    /// </exception>
    public async Task<RegisteredUser> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await FindBySignUpIdAsync(command.SignUpId, cancellationToken) is { } existing)
        {
            return new RegisteredUser(existing, Created: false);
        }

        User user = User.Register(
            command.UserId,
            command.SignUpId,
            personalData.Protect(command.FirstName.Value),
            personalData.Protect(command.LastName.Value),
            Protect(command.Phone.Value, lookupValue: command.Phone.Value),
            Protect(command.Email.Value, lookupValue: command.Email.LookupValue),
            command.Locale,
            command.TermsVersion,
            command.PrivacyVersion,
            command.AcceptedAt);
        database.Users.Add(user);

        WelcomeMessage? welcome = await TryEnqueueWelcomeAsync(user.Id, command, cancellationToken).ConfigureAwait(false);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.GetBaseException() is PostgresException postgres)
        {
            Forget(user);
            if (welcome is not null)
            {
                welcomeSender.Discard(welcome);
            }

            switch (postgres.ConstraintName)
            {
                case UsersSchema.SignUpIdUnique:
                    return new RegisteredUser(
                        await database.Users
                            .Where(candidate => candidate.SignUpId == command.SignUpId)
                            .Select(candidate => candidate.Id)
                            .SingleAsync(cancellationToken),
                        Created: false);
                case UsersSchema.PhoneLookupHashUnique:
                    throw new RefusalException(MoniPayErrorTypes.PhoneAlreadyRegistered);
                case UsersSchema.EmailLookupHashUnique:
                    throw new RefusalException(MoniPayErrorTypes.EmailAlreadyRegistered);
                default:
                    throw;
            }
        }

        return new RegisteredUser(user.Id, Created: true);
    }

    /// <summary>
    /// Renders and stages the welcome before the registration save. The optional work is the
    /// only thing the catch covers: a failure logs one warning and leaves the user to be created
    /// without a welcome, and a later replay does not backfill it.
    /// </summary>
    private async Task<WelcomeMessage?> TryEnqueueWelcomeAsync(
        UserId userId,
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            WelcomeMessage welcome = welcomeRenderer.Render(userId, command.Email, command.FirstName, command.Locale);
            await welcomeSender.EnqueueAsync(welcome, cancellationToken).ConfigureAwait(false);
            return welcome;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            UsersLog.WelcomeMessageEnqueueFailed(logger, userId);
            return null;
        }
    }

    private ProtectedContact Protect(string displayValue, string lookupValue) =>
        new(personalData.Protect(displayValue), lookupDigest.Compute(lookupValue));

    private Task<UserId?> FindBySignUpIdAsync(SignUpId signUpId, CancellationToken cancellationToken) =>
        database.Users
            .Where(user => user.SignUpId == signUpId)
            .Select(user => (UserId?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);

    private void Forget(User user)
    {
        foreach (UserConsent consent in user.Consents.ToArray())
        {
            database.Entry(consent).State = EntityState.Detached;
        }

        database.Entry(user).State = EntityState.Detached;
    }
}
