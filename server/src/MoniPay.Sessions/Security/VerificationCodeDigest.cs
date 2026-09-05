using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;

namespace MoniPay.Sessions.Security;

/// <summary>
/// The keyed digest a sign-up stores instead of its verification code:
/// <c>HMAC-SHA256(verificationCodeKey, signUpId || phoneLookupHash || code)</c>. Binding the
/// code to the sign-up and the phone stops a digest harvested from one sign-up from checking
/// a code against another. Comparison is the aggregate's
/// <see cref="CryptographicOperations.FixedTimeEquals"/>.
/// </summary>
internal sealed class VerificationCodeDigest(IOptions<SessionsOptions> options)
{
    private readonly byte[] key = options.Value.VerificationCodeKey;

    public byte[] Compute(SignUpId signUpId, LookupHash phoneLookupHash, string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        using IncrementalHash hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key);
        // RFC 4122 byte order: the same digest is reproducible outside the CLR (SQL, tooling).
        hmac.AppendData(signUpId.Value.ToByteArray(bigEndian: true));
        hmac.AppendData(phoneLookupHash.Value);
        hmac.AppendData(Encoding.UTF8.GetBytes(code));
        return hmac.GetHashAndReset();
    }
}
