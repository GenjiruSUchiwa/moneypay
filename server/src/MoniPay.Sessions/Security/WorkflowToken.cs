namespace MoniPay.Sessions.Security;

/// <summary>
/// A workflow token in the only instant its raw value exists: the response that hands it to the
/// client. Only <see cref="Digest"/> may ever be persisted, stored or logged.
/// </summary>
internal sealed record WorkflowToken(string Raw, byte[] Digest)
{
    // The record's generated ToString would print Raw; a logged token is a leaked credential.
    public override string ToString() => nameof(WorkflowToken);
}
