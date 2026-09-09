using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;

namespace MoniPay.Sessions.Security;

internal enum SignUpTokenPurpose
{
    SignUp,
    Registration,
}

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
        hmac.AppendData(signUpId.Value.ToByteArray(bigEndian: true));
        hmac.AppendData(Encoding.UTF8.GetBytes(rawToken));
        return hmac.GetHashAndReset();
    }
}
