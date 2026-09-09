using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Providers;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Features.SignUps.ResendCode;

internal sealed class CreateVerificationCodeDeliveryHandler(
    MoniPayDbContext database,
    IVerificationCodeSender sender,
    VerificationCodeGenerator codes,
    VerificationCodeDigest codeDigest,
    SignUpPersonalDataProtector personalData,
    VerificationCodeRenderer renderer,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options)
{
    private readonly SessionsOptions settings = options.Value;

    public async Task<CreateVerificationCodeDeliveryResult> HandleAsync(SignUpId signUpId, CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        await using IDbContextTransaction transaction = await database.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        SignUp signUp = await database.LockSignUpAsync(signUpId, cancellationToken).ConfigureAwait(false);

        string code = codes.Next();
        signUp.RotateVerificationCode(codeDigest.Compute(signUp.Id, signUp.PhoneLookupHash, code), now, settings);
        PhoneNumber recipient = new(personalData.Unprotect(signUp.PhoneCiphertext));
        VerificationCodeMessage message = signUp.ComposeVerificationCodeMessage(recipient, code, settings, renderer);
        await sender.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        await database.CommitAsync(transaction, cancellationToken).ConfigureAwait(false);

        return new(signUp.Id, message.ExpiresAt, signUp.CanResendAt, signUp.ExpiresAt);
    }
}
