namespace MoniPay.Sessions.Security;

internal sealed record WorkflowToken(string Raw, byte[] Digest)
{
    public override string ToString() => nameof(WorkflowToken);
}
