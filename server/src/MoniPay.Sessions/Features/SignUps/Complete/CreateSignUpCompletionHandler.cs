using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Storage;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Features.SignUps.Complete;

/// <summary>
/// Completes a sign-up in one transaction: the registration token is checked in constant time,
/// the user is provisioned, the bootstrap session is opened and the sign-up is marked completed,
/// or nothing is written. A retry on a completed sign-up replaces only the bootstrap session.
/// </summary>
internal sealed class CreateSignUpCompletionHandler(
    MoniPayDbContext database,
    IUserProvisioning users,
    SignUpTokens tokens,
    SessionTokenService sessions,
    SignUpPersonalDataProtector personalData,
    TimeProvider timeProvider)
{
    public async Task<CreateSignUpCompletionResult> HandleAsync(
        SignUpId signUpId,
        string registrationToken,
        CreateSignUpCompletionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(registrationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();

        await using IDbContextTransaction transaction = await database.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        SignUp signUp = await database.LockSignUpAsync(signUpId, cancellationToken).ConfigureAwait(false);

        byte[] presented = tokens.Digest(SignUpTokenPurpose.Registration, signUp.Id, registrationToken);
        if (signUp.RegistrationTokenDigest is not { } stored
            || !CryptographicOperations.FixedTimeEquals(presented, stored))
        {
            throw new RefusalException(MoniPayErrorTypes.RegistrationTokenInvalid);
        }

        if (signUp.Status == SignUpStatus.Completed)
        {
            if (signUp.ProvisionedUserId is not { } userId || signUp.BootstrapSessionId is not { } priorSessionId)
            {
                throw new RefusalException(MoniPayErrorTypes.SignUpStateInvalid);
            }

            SessionTokenResult replacement = await sessions
                .ReplaceBootstrapAsync(userId, priorSessionId, command.DeviceId, cancellationToken)
                .ConfigureAwait(false);
            signUp.Complete(userId, replacement.SessionId, now);
            await database.CommitAsync(transaction, cancellationToken).ConfigureAwait(false);

            return new(Created: false, replacement);
        }

        PhoneNumber phone = new(personalData.Unprotect(signUp.PhoneCiphertext));
        ProvisionedUser provisioned = await users.ProvisionAsync(
            new ProvisionUserRequest(
                signUp.Id,
                UserId.New(),
                phone,
                command.FirstName,
                command.LastName,
                command.Email,
                signUp.Locale,
                signUp.TermsVersion,
                signUp.PrivacyVersion,
                now),
            cancellationToken).ConfigureAwait(false);
        SessionTokenResult session = await sessions
            .CreateAsync(provisioned.Id, command.DeviceId, cancellationToken)
            .ConfigureAwait(false);
        signUp.Complete(provisioned.Id, session.SessionId, now);
        await database.CommitAsync(transaction, cancellationToken).ConfigureAwait(false);

        return new(Created: true, session);
    }
}
