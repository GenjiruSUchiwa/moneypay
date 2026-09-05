using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Ports;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Features.SignUps.VerifyPhone;

/// <summary>
/// Checks a code against a locked sign-up row. A mismatch is committed before it is refused, so
/// the attempt budget survives the refusal; a match asks Users whether the phone is taken before
/// it publishes the registration token.
/// </summary>
internal sealed class CreatePhoneVerificationHandler(
    MoniPayDbContext database,
    VerificationCodeDigest codeDigest,
    SignUpTokens tokens,
    SignUpPersonalDataProtector personalData,
    IRegisteredPhoneLookup registeredPhones,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options)
{
    /// <summary>The request member a mismatch points at, as the HTTP contract names it.</summary>
    internal const string VerificationCodePointer = "/data/attributes/verificationCode";

    private readonly SessionsOptions settings = options.Value;

    public async Task<CreatePhoneVerificationResult> HandleAsync(
        SignUpId signUpId,
        string verificationCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(verificationCode);
        DateTimeOffset now = timeProvider.GetUtcNow();

        await using IDbContextTransaction transaction = await database.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        SignUp signUp = await database.LockSignUpAsync(signUpId, cancellationToken).ConfigureAwait(false);

        PhoneVerificationOutcome outcome = signUp.VerifyPhone(
            codeDigest.Compute(signUp.Id, signUp.PhoneLookupHash, verificationCode),
            now,
            settings);
        if (outcome != PhoneVerificationOutcome.Verified)
        {
            await database.CommitAsync(transaction, cancellationToken).ConfigureAwait(false);
            throw outcome == PhoneVerificationOutcome.Locked
                ? signUp.AttemptLimitRefusal(now)
                : new RefusalException(MoniPayErrorTypes.VerificationCodeInvalid, pointers: [VerificationCodePointer]);
        }

        PhoneNumber phone = new(personalData.Unprotect(signUp.PhoneCiphertext));
        if (await registeredPhones.FindUserIdAsync(phone, cancellationToken).ConfigureAwait(false) is not null)
        {
            // Nothing can complete this sign-up, so it is closed now rather than at expiry:
            // left open, it would own the phone and refuse every start until then.
            signUp.Close();
            await database.CommitAsync(transaction, cancellationToken).ConfigureAwait(false);
            throw new RefusalException(MoniPayErrorTypes.PhoneAlreadyRegistered);
        }

        WorkflowToken registrationToken = tokens.Issue(signUp.Id, SignUpTokenPurpose.Registration);
        signUp.IssueRegistrationToken(registrationToken.Digest);
        await database.CommitAsync(transaction, cancellationToken).ConfigureAwait(false);

        return new(signUp.Id, registrationToken.Raw, signUp.ExpiresAt);
    }
}
