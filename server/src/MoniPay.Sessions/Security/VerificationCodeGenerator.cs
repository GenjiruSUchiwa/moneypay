using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace MoniPay.Sessions.Security;

internal sealed class VerificationCodeGenerator(IOptions<SessionsOptions> options)
{
    private readonly int length = options.Value.VerificationCodeLength;

    public string Next() => RandomNumberGenerator.GetString("0123456789", length);
}
