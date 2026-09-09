using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Users.Domain;
using MoniPay.Users.Persistence;
using MoniPay.Users.Providers;
using MoniPay.Users.Security;
using Npgsql;

namespace MoniPay.Users.Features.Registration;

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

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.GetBaseException() is PostgresException postgres)
        {
            Forget(user);
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

        await TryEnqueueWelcomeAsync(user.Id, command, cancellationToken).ConfigureAwait(false);

        return new RegisteredUser(user.Id, Created: true);
    }

    private async Task TryEnqueueWelcomeAsync(
        UserId userId,
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            WelcomeMessage welcome = welcomeRenderer.Render(userId, command.Email, command.FirstName, command.Locale);
            await welcomeSender.EnqueueAsync(welcome, cancellationToken).ConfigureAwait(false);
            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            UsersLog.WelcomeMessageEnqueueFailed(logger, userId);
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
