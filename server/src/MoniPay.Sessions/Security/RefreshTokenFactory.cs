using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace MoniPay.Sessions.Security;

/// <summary>
/// Mints refresh tokens: 32 random bytes, base64url — 43 characters a URL, a JSON body or a
/// database round trip survives unchanged. Only the SHA-256 digest is ever stored; the raw
/// value exists once, in the response that hands it to the client.
/// </summary>
internal sealed class RefreshTokenFactory
{
    /// <summary>The base64url encoding of 32 bytes, without padding.</summary>
    public const int RawLength = 43;

    public (string Raw, byte[] Digest) Create()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        string raw = Base64Url.EncodeToString(bytes);
        return (raw, Digest(raw));
    }

    /// <summary>The digest a stored token is looked up and replayed by.</summary>
    public byte[] Digest(string raw) => SHA256.HashData(Encoding.UTF8.GetBytes(raw));
}
