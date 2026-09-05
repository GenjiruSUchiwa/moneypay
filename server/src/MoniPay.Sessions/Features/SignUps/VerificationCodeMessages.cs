using MoniPay.Kernel;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Providers;

namespace MoniPay.Sessions.Features.SignUps;

/// <summary>
/// What the start and resend slices hand the delivery port: the code in its raw parts, bound to
/// the code's expiry so a late delivery is dropped rather than sent.
/// </summary>
internal static class VerificationCodeMessages
{
    private const string IdempotencyKeyPrefix = "verification-code";

    extension(SignUp signUp)
    {
        /// <summary>
        /// The message for the code the sign-up holds right now: its expiry is the row's, never a
        /// second computation of it.
        /// </summary>
        public VerificationCodeMessage ComposeVerificationCodeMessage(
            PhoneNumber recipient,
            string code,
            SessionsOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return new(
                signUp.Id,
                recipient,
                code,
                options.VerificationCodeLifetime,
                signUp.Locale,
                signUp.CodeExpiresAt ?? throw new InvalidOperationException("The sign-up holds no code to deliver."),
                FormattableString.Invariant($"{IdempotencyKeyPrefix}:{signUp.Id}:{signUp.ResendCount}"));
        }
    }
}
