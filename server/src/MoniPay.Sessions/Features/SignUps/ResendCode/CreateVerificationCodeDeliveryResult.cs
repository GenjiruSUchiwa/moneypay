using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.ResendCode;

internal sealed record CreateVerificationCodeDeliveryResult(
    SignUpId SignUpId,
    DateTimeOffset CodeExpiresAt,
    DateTimeOffset CanResendAt,
    DateTimeOffset SignUpExpiresAt);
