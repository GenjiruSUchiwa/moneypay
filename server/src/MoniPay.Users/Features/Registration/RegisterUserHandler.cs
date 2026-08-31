using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Users.Domain;
using MoniPay.Users.Persistence;
using MoniPay.Users.Security;
using Npgsql;

namespace MoniPay.Users.Features.Registration;

/// <summary>
/// Provisions exactly one user per sign-up: encrypts the contact data, computes the keyed
/// lookup hashes, and writes the user with its two consent rows. Idempotent on the sign-up id.
/// It saves inside the caller's ambient transaction — that is how it observes a
/// unique-constraint violation and maps it — but it opens no transaction of its own and never
/// commits: the ambient transaction decides.
/// </summary>
public sealed class RegisterUserHandler
{
    private readonly MoniPayDbContext database;
    private readonly UserPersonalDataProtector personalData;
    private readonly UserLookupDigest lookupDigest;

    internal RegisterUserHandler(
        MoniPayDbContext database,
        UserPersonalDataProtector personalData,
        UserLookupDigest lookupDigest)
    {
        this.database = database;
        this.personalData = personalData;
        this.lookupDigest = lookupDigest;
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
                    // Another registration of this sign-up won the race and committed — the
                    // violation proves it. SaveChanges already rolled the ambient transaction
                    // back to its own savepoint, so the winner is readable here.
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

    private ProtectedContact Protect(string displayValue, string lookupValue) =>
        new(personalData.Protect(displayValue), lookupDigest.Compute(lookupValue));

    private Task<UserId?> FindBySignUpIdAsync(SignUpId signUpId, CancellationToken cancellationToken) =>
        database.Users
            .Where(user => user.SignUpId == signUpId)
            .Select(user => (UserId?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);

    private void Forget(User user)
    {
        // The failed insert must not ride along on the caller's next save. Detaching triggers
        // EF's navigation fixup, which edits the consents collection — hence the copy.
        foreach (UserConsent consent in user.Consents.ToArray())
        {
            database.Entry(consent).State = EntityState.Detached;
        }

        database.Entry(user).State = EntityState.Detached;
    }
}
