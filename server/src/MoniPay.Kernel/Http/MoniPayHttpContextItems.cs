namespace MoniPay.Kernel.Http;

/// <summary>
/// The <c>HttpContext.Items</c> keys the host and the modules share within one request, where a
/// typed feature would be heavier. The values never cross the wire.
/// </summary>
public static class MoniPayHttpContextItems
{
    /// <summary>
    /// The stable authentication problem code the scheme selected server-side, so the challenge
    /// body names the credential instead of a generic 401. A handler writes its own scheme's code;
    /// the submitted authorization scheme never decides it.
    /// </summary>
    public const string AuthenticationProblemCode = "MoniPay.AuthenticationProblemCode";
}
