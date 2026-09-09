using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Providers;

namespace MoniPay.Sessions.Features.SignUps.Get;

internal sealed record SignUpView(
    SignUpStatus Status,
    CodeDeliveryState? CodeDelivery,
    DateTimeOffset? CodeExpiresAt,
    DateTimeOffset CanResendAt,
    DateTimeOffset SignUpExpiresAt);
