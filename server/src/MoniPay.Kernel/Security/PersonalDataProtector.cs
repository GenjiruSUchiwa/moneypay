using System.Security.Cryptography;
using System.Text;

namespace MoniPay.Kernel.Security;

/// <summary>
/// AES-GCM over a module's personal-data key. The payload starts with a one-byte key version —
/// authenticated as associated data — so a later re-encryption migration can find rows by key.
/// Each module derives its own protector from its own key; the layout is shared, the key never is.
/// </summary>
public class PersonalDataProtector(byte[] key)
{
    private readonly byte[] key = Base64Key.Require(key);

    private const byte KeyVersion = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;

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
        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(ciphertext.Value);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("The ciphertext is not valid base64.", exception);
        }

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
