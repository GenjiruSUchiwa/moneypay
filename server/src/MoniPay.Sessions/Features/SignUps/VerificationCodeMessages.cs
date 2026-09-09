using MoniPay.Kernel;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Providers;

namespace MoniPay.Sessions.Features.SignUps;

internal static class VerificationCodeMessages
{
    private const string IdempotencyKeyPrefix = "verification-code";

    extension(SignUp signUp)
    {
        public VerificationCodeMessage ComposeVerificationCodeMessage(
            PhoneNumber recipient,
            string code,
            SessionsOptions options,
            VerificationCodeRenderer renderer)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(renderer);

            return new(
                signUp.Id,
                recipient,
                renderer.Render(signUp.Locale, code, options.VerificationCodeLifetime),
                signUp.CodeExpiresAt ?? throw new InvalidOperationException("The sign-up holds no code to deliver."),
                FormattableString.Invariant($"{IdempotencyKeyPrefix}:{signUp.Id}:{signUp.ResendCount}"));
        }
    }
}
