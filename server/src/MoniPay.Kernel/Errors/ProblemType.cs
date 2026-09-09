using System.Net;

namespace MoniPay.Kernel.Errors;

public sealed record ProblemType
{
    public string Code { get; }

    public HttpStatusCode Status { get; }

    public string Urn => MoniPayErrorTypes.Prefix + Code;

    public ProblemType(string code, HttpStatusCode status)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        Code = code;
        Status = status;
    }
}
