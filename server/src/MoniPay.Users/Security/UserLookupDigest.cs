using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;

namespace MoniPay.Users.Security;

/// <summary>
/// The keyed hash a contact value is looked up by: HMAC-SHA256 over the normalized value. The
/// HMAC key is HKDF-derived from the personal-data key, so lookup and encryption never share
/// key material directly.
/// </summary>
internal sealed class UserLookupDigest(IOptions<UsersOptions> options)
{
    private static readonly byte[] LookupKeyInfo = "MoniPay.Users:lookup"u8.ToArray();

    private readonly byte[] lookupKey = HKDF.DeriveKey(
        HashAlgorithmName.SHA256,
        options.Value.PersonalDataKey,
        outputLength: 32,
        info: LookupKeyInfo);

    public LookupHash Compute(string normalizedValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(normalizedValue);

        return new(HMACSHA256.HashData(lookupKey, Encoding.UTF8.GetBytes(normalizedValue)));
    }
}
