using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;

namespace MoniPay.Sessions.Security;

internal sealed class VerificationCodeDigest(IOptions<SessionsOptions> options)
{
    private readonly byte[] key = options.Value.VerificationCodeKey;

    public byte[] Compute(SignUpId signUpId, LookupHash phoneLookupHash, string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        using IncrementalHash hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key);
        hmac.AppendData(signUpId.Value.ToByteArray(bigEndian: true));
        hmac.AppendData(phoneLookupHash.Value);
        hmac.AppendData(Encoding.UTF8.GetBytes(code));
        return hmac.GetHashAndReset();
    }
}
