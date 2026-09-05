namespace MoniPay.Kernel.Security;

/// <summary>
/// A 32-byte secret as configuration carries it: base64 text. Every module key — AES, HMAC,
/// signing — is validated and decoded through here, so the rule exists once.
/// </summary>
public static class Base64Key
{
    public const int Size = 32;

    /// <summary>Reports whether <paramref name="base64"/> decodes to exactly 32 bytes.</summary>
    public static bool IsValid(string? base64)
    {
        Span<byte> key = stackalloc byte[Size];
        return base64 is not null
            && Convert.TryFromBase64String(base64, key, out int written)
            && written == Size;
    }

    /// <summary>Returns <paramref name="key"/> if it is exactly 32 bytes; a primitive keyed by
    /// an empty or short array would run and produce valid-looking output.</summary>
    public static byte[] Require(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfNotEqual(key.Length, Size, nameof(key));
        return key;
    }

    /// <summary>Decodes a key the options validation already accepted, refusing any other length.</summary>
    public static byte[] Decode(string base64) => Require(Convert.FromBase64String(base64));
}
