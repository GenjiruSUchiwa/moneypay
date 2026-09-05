using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;

namespace MoniPay.Sessions.Security;

/// <summary>
/// Which scheme a workflow token belongs to. The purposes are distinct byte strings, so one raw
/// value cannot serve as both a sign-up and a registration token.
/// </summary>
internal enum SignUpTokenPurpose
{
    SignUp,
    Registration,
}

/// <summary>
/// Issues and digests workflow tokens. A token is 32 random bytes as base64url without padding
/// (43 characters); the raw value is returned exactly once, and what a sign-up stores is
/// <c>HMAC-SHA256(verificationCodeKey, purpose || signUpId || rawToken)</c>: the purpose binds
/// the digest to one scheme and the identifier binds it to one sign-up.
/// </summary>
internal sealed class SignUpTokens(IOptions<SessionsOptions> options)
{
    private const int TokenSize = 32;

    private readonly byte[] key = options.Value.VerificationCodeKey;

    public WorkflowToken Issue(SignUpId signUpId, SignUpTokenPurpose purpose)
    {
        string raw = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSize));
        return new(raw, Digest(purpose, signUpId, raw));
    }

    public byte[] Digest(SignUpTokenPurpose purpose, SignUpId signUpId, string rawToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawToken);

        using IncrementalHash hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key);
        hmac.AppendData(purpose switch
        {
            SignUpTokenPurpose.SignUp => "signup"u8,
            SignUpTokenPurpose.Registration => "registration"u8,
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null),
        });
        // RFC 4122 byte order: the same digest is reproducible outside the CLR (SQL, tooling).
        hmac.AppendData(signUpId.Value.ToByteArray(bigEndian: true));
        hmac.AppendData(Encoding.UTF8.GetBytes(rawToken));
        return hmac.GetHashAndReset();
    }
}
