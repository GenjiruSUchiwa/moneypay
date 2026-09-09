using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Sessions;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

public sealed class WorkflowTokenTests
{
    private const int SampleCount = 10_000;

    private readonly SignUpTokens tokens = new(WithKey(TestKeys.VerificationCode));

    [Fact]
    public void A_token_is_43_base64url_characters_without_padding()
    {
        WorkflowToken token = tokens.Issue(SignUpId.New(), SignUpTokenPurpose.SignUp);

        Assert.Equal(43, token.Raw.Length);
        Assert.DoesNotContain('+', token.Raw);
        Assert.DoesNotContain('/', token.Raw);
        Assert.DoesNotContain('=', token.Raw);
    }

    [Fact]
    public void Raw_tokens_are_unique_across_a_large_sample()
    {
        HashSet<string> rawTokens = [];

        for (int i = 0; i < SampleCount; i++)
        {
            Assert.True(rawTokens.Add(tokens.Issue(SignUpId.New(), SignUpTokenPurpose.SignUp).Raw));
        }
    }

    [Fact]
    public void The_digest_is_bound_to_the_issued_sign_up_and_purpose()
    {
        SignUpId signUpId = SignUpId.New();
        WorkflowToken token = tokens.Issue(signUpId, SignUpTokenPurpose.SignUp);

        Assert.Equal(tokens.Digest(SignUpTokenPurpose.SignUp, signUpId, token.Raw), token.Digest);
        Assert.NotEqual(tokens.Digest(SignUpTokenPurpose.Registration, signUpId, token.Raw), token.Digest);
        Assert.NotEqual(tokens.Digest(SignUpTokenPurpose.SignUp, SignUpId.New(), token.Raw), token.Digest);
    }

    [Fact]
    public void One_raw_value_never_digests_the_same_for_both_purposes()
    {
        SignUpId signUpId = SignUpId.New();
        string rawToken = tokens.Issue(signUpId, SignUpTokenPurpose.SignUp).Raw;

        byte[] signUpDigest = tokens.Digest(SignUpTokenPurpose.SignUp, signUpId, rawToken);
        byte[] registrationDigest = tokens.Digest(SignUpTokenPurpose.Registration, signUpId, rawToken);

        Assert.NotEqual(signUpDigest, registrationDigest);
    }

    private static IOptions<SessionsOptions> WithKey(string keyBase64) =>
        Options.Create(new SessionsOptions { VerificationCodeKeyBase64 = keyBase64 });
}
