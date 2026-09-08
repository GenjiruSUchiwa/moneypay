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
        DateTimeOffset acceptedAt = timeProvider.GetUtcNow();

        await using IDbContextTransaction transaction = await database.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        SignUp signUp = await database.LockSignUpAsync(signUpId, cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = timeProvider.GetUtcNow();

        byte[] presented = tokens.Digest(SignUpTokenPurpose.Registration, signUp.Id, registrationToken);
        if (signUp.RegistrationTokenDigest is not { } stored
            || !CryptographicOperations.FixedTimeEquals(presented, stored))
        {
            throw new RefusalException(MoniPayErrorTypes.RegistrationTokenInvalid);
        }

        if (signUp.CompletionRefusal(now) is { } refusal)
        {
            throw new RefusalException(refusal);
        }

        bool created = signUp.Status == SignUpStatus.PhoneVerified;
        SessionTokenResult session;
        if (created)
        {
            PhoneNumber phone = new(personalData.Unprotect(signUp.PhoneCiphertext));
            UserId userId = await users.ProvisionAsync(
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
                    acceptedAt),
                cancellationToken).ConfigureAwait(false);
            session = await sessions.CreateAsync(userId, command.DeviceId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (signUp.ProvisionedUserId is not { } userId || signUp.BootstrapSessionId is not { } priorSessionId)
            {
                throw new RefusalException(MoniPayErrorTypes.SignUpStateInvalid);
            }

            session = await sessions
                .ReplaceBootstrapAsync(userId, priorSessionId, command.DeviceId, cancellationToken)
                .ConfigureAwait(false);
        }

        signUp.Complete(session.UserId, session.SessionId, now);
        await database.CommitAsync(transaction, cancellationToken).ConfigureAwait(false);

        return new(created, session);
    }
}
