using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.ResendCode;

/// <summary>The timing a resend refreshed. The code itself travels only through the delivery port.</summary>
internal sealed record CreateVerificationCodeDeliveryResult(
    SignUpId SignUpId,
    DateTimeOffset CodeExpiresAt,
    DateTimeOffset CanResendAt,
    DateTimeOffset SignUpExpiresAt);
