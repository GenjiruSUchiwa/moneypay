using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace MoniPay.Sessions.Security;

/// <summary>
/// Draws a verification code from <see cref="RandomNumberGenerator.GetString"/>: unbiased, and
/// leading zeros are kept. Never <see cref="Random"/>, a timestamp or a sequence — the code is
/// the credential. The length is the validated <see cref="SessionsOptions.VerificationCodeLength"/>,
/// so the initial code and every resend agree.
/// </summary>
internal sealed class VerificationCodeGenerator(IOptions<SessionsOptions> options)
{
    private readonly int length = options.Value.VerificationCodeLength;

    public string Next() => RandomNumberGenerator.GetString("0123456789", length);
}
