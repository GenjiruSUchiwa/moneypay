using System.Net;

namespace MoniPay.Kernel.Errors;

/// <summary>
/// A stable problem code bound to the HTTP status the contract assigns it. Binding them in one
/// value keeps a refusal from pairing a code with a status the contract does not allow.
/// </summary>
public sealed record ProblemType
{
    /// <summary>The stable problem code, the suffix of the problem-type URN.</summary>
    public string Code { get; }

    /// <summary>The HTTP status the host returns for this problem.</summary>
    public HttpStatusCode Status { get; }

    /// <summary>The absolute problem-type URN.</summary>
    public string Urn => MoniPayErrorTypes.Prefix + Code;

    public ProblemType(string code, HttpStatusCode status)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        Code = code;
        Status = status;
    }
}
