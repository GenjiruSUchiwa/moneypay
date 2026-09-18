using MoniPay.Kernel;

namespace MoniPay.Sessions.Providers;

public enum SecurityAlertKind
{
    SessionRevoked,
    RefreshTokenReuseDetected,
}

public sealed record SecurityAlert(
    UserId UserId,
    SecurityAlertKind Kind,
    DateTimeOffset OccurredAt,
    string IdempotencyKey)
{
    public override string ToString() => nameof(SecurityAlert);
}
