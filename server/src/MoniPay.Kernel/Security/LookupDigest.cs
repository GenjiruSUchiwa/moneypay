using System.Security.Cryptography;
using System.Text;

namespace MoniPay.Kernel.Security;

/// <summary>
/// The keyed hash a protected value is looked up by: HMAC-SHA256 over the normalized value,
/// keyed by an HKDF derivation of the module's personal-data key under a module-specific
/// <paramref name="info"/>. Lookup and encryption never share key material directly, and two
/// modules never produce the same hash for the same value.
/// </summary>
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
