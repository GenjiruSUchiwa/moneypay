using System.Security.Cryptography;
using System.Text;

namespace MoniPay.Kernel.Security;

public class LookupDigest(byte[] personalDataKey, byte[] info)
{
    private readonly byte[] lookupKey = HKDF.DeriveKey(
        HashAlgorithmName.SHA256,
        Base64Key.Require(personalDataKey),
        outputLength: Base64Key.Size,
        info: info);

    public LookupHash Compute(string normalizedValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(normalizedValue);

        return new(HMACSHA256.HashData(lookupKey, Encoding.UTF8.GetBytes(normalizedValue)));
    }
}
