namespace MoniPay.Sessions.Domain;

internal enum SessionRevokeReason
{
    UserRequest,

    RefreshTokenReuse,

    BootstrapReplaced,
}
