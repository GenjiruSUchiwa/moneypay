using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;
using MoniPay.Sessions.Providers;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Features.SignUps.Start;

internal sealed class StartSignUpHandler(
    MoniPayDbContext database,
    IVerificationCodeSender sender,
    VerificationCodeGenerator codes,
    VerificationCodeDigest codeDigest,
    SignUpTokens tokens,
    SignUpPersonalDataProtector personalData,
    PhoneLookupDigest phoneLookup,
    VerificationCodeRenderer renderer,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options)
{
    private readonly SessionsOptions settings = options.Value;

    public async Task<StartSignUpResult> HandleAsync(StartSignUpCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        DateTimeOffset now = timeProvider.GetUtcNow();
        LookupHash phoneHash = phoneLookup.Compute(command.Phone);

        await using IDbContextTransaction transaction = await database.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await database.LockPhoneAsync(phoneHash, cancellationToken).ConfigureAwait(false);

        SignUp? active = await ExpireOrFindActiveAsync(phoneHash, now, cancellationToken).ConfigureAwait(false);
        if (active is null)
        {
            await RefuseAboveTheStartLimitAsync(phoneHash, now, cancellationToken).ConfigureAwait(false);
        }

        string code = codes.Next();
        (SignUp signUp, WorkflowToken token) = active is null
            ? StartNew(command, phoneHash, code, now)
            : Reopen(active, phoneHash, code, now);

        VerificationCodeMessage message = signUp.ComposeVerificationCodeMessage(command.Phone, code, settings, renderer);
        await sender.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        await database.CommitAsync(transaction, cancellationToken).ConfigureAwait(false);

        return new(signUp.Id, token.Raw, message.ExpiresAt, signUp.CanResendAt, signUp.ExpiresAt);
    }

    private async Task RefuseAboveTheStartLimitAsync(LookupHash phoneHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        DateTimeOffset windowStart = now - settings.StartWindow;
        List<DateTimeOffset> starts = await database.SignUps
            .Where(signUp => signUp.PhoneLookupHash.Equals(phoneHash) && signUp.CreatedAt > windowStart)
            .Select(signUp => signUp.CreatedAt)
            .OrderBy(createdAt => createdAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (starts.Count >= settings.MaximumStartsPerWindow)
        {
            DateTimeOffset freeing = starts[^settings.MaximumStartsPerWindow];
            throw new RefusalException(MoniPayErrorTypes.RateLimited, retryAfter: freeing + settings.StartWindow - now);
        }
    }

    private async Task<SignUp?> ExpireOrFindActiveAsync(LookupHash phoneHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        SignUp? active = await database.LockActiveSignUpAsync(phoneHash, cancellationToken).ConfigureAwait(false);
        if (active is null || !active.TryExpire(now))
        {
            return active;
        }

        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return null;
    }

    private (SignUp SignUp, WorkflowToken Token) StartNew(StartSignUpCommand command, LookupHash phoneHash, string code, DateTimeOffset now)
    {
        SignUpId id = SignUpId.New();
        WorkflowToken token = tokens.Issue(id, SignUpTokenPurpose.SignUp);
        SignUp signUp = SignUp.Start(
            id,
            personalData.Protect(command.Phone.Value),
            phoneHash,
            command.Locale,
            command.TermsVersion,
            command.PrivacyVersion,
            codeDigest.Compute(id, phoneHash, code),
            token.Digest,
            now,
            settings);
        database.SignUps.Add(signUp);
        return (signUp, token);
    }

    private (SignUp SignUp, WorkflowToken Token) Reopen(SignUp active, LookupHash phoneHash, string code, DateTimeOffset now)
    {
        WorkflowToken token = tokens.Issue(active.Id, SignUpTokenPurpose.SignUp);
        active.Restart(codeDigest.Compute(active.Id, phoneHash, code), token.Digest, now, settings);
        return (active, token);
    }
}
