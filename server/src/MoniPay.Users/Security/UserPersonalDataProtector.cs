using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MoniPay.Users.Domain;

namespace MoniPay.Users.Security;

/// <summary>
/// AES-GCM over the Users personal-data key, for first name, last name, phone and email. The
/// payload starts with a one-byte key version — authenticated as associated data — so a later
/// re-encryption migration can find rows by key.
/// </summary>
internal sealed class UserPersonalDataProtector(IOptions<UsersOptions> options)
{
    private const byte KeyVersion = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] key = options.Value.PersonalDataKey;

    public Ciphertext Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] payload = new byte[1 + NonceSize + plainBytes.Length + TagSize];
        payload[0] = KeyVersion;
        RandomNumberGenerator.Fill(payload.AsSpan(1, NonceSize));

        using AesGcm cipher = new(key, TagSize);
        cipher.Encrypt(
            nonce: payload.AsSpan(1, NonceSize),
            plaintext: plainBytes,
            ciphertext: payload.AsSpan(1 + NonceSize, plainBytes.Length),
            tag: payload.AsSpan(payload.Length - TagSize),
            associatedData: payload.AsSpan(0, 1));

        return new(Convert.ToBase64String(payload));
    }

    public string Unprotect(Ciphertext ciphertext)
    {
        byte[] payload = Convert.FromBase64String(ciphertext.Value);
        if (payload.Length < 1 + NonceSize + TagSize || payload[0] != KeyVersion)
        {
            throw new CryptographicException("The ciphertext does not carry the current personal-data key version.");
        }

        byte[] plainBytes = new byte[payload.Length - 1 - NonceSize - TagSize];
        using AesGcm cipher = new(key, TagSize);
        cipher.Decrypt(
            nonce: payload.AsSpan(1, NonceSize),
            ciphertext: payload.AsSpan(1 + NonceSize, plainBytes.Length),
            tag: payload.AsSpan(payload.Length - TagSize),
            plaintext: plainBytes,
            associatedData: payload.AsSpan(0, 1));

        return Encoding.UTF8.GetString(plainBytes);
    }
}
