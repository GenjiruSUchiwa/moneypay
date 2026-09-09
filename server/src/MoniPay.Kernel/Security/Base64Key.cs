namespace MoniPay.Kernel.Security;

public static class Base64Key
{
    public const int Size = 32;

    public static bool IsValid(string? base64)
    {
        Span<byte> key = stackalloc byte[Size];
        return base64 is not null
            && Convert.TryFromBase64String(base64, key, out int written)
            && written == Size;
    }

    public static byte[] Require(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfNotEqual(key.Length, Size, nameof(key));
        return key;
    }

    public static byte[] Decode(string base64) => Require(Convert.FromBase64String(base64));
}
