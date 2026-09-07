namespace MoniPay.Sessions.Security;

/// <summary>
/// The signed access JWT in the only instant its raw value exists: the result handed to the
/// client. Only <see cref="Value"/> may ever leave the process; nothing stores or logs it.
/// </summary>
internal sealed record AccessToken(string Value)
{
    // The record's generated ToString would print Value; a logged token is a leaked credential.
    public override string ToString() => nameof(AccessToken);
}
