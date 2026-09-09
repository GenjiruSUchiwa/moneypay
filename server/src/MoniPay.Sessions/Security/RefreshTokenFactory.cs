using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace MoniPay.Sessions.Security;

internal sealed class RefreshTokenFactory
{
    public const int RawLength = 43;

    public (string Raw, byte[] Digest) Create()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        string raw = Base64Url.EncodeToString(bytes);
        return (raw, Digest(raw));
    }

    public byte[] Digest(string raw) => SHA256.HashData(Encoding.UTF8.GetBytes(raw));
}
