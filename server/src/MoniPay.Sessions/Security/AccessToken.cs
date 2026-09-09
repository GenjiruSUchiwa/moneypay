namespace MoniPay.Sessions.Security;

internal sealed record AccessToken(string Value)
{
    public override string ToString() => nameof(AccessToken);
}
