using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Providers;

namespace MoniPay.Sessions.Features.SignUps.Get;

/// <summary>What a client may read back about its sign-up: state and timing, never a token.</summary>
internal sealed record SignUpView(
    SignUpStatus Status,
    CodeDeliveryState? CodeDelivery,
    DateTimeOffset? CodeExpiresAt,
    DateTimeOffset CanResendAt,
    DateTimeOffset SignUpExpiresAt);
