using System.Text;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Sessions;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

public sealed class VerificationCodeDigestTests
{
    private readonly VerificationCodeDigest digest = Digest(TestKeys.VerificationCode);

    [Fact]
    public void The_same_sign_up_phone_and_code_give_the_same_digest()
    {
        SignUpId signUpId = SignUpId.New();
        byte[] first = digest.Compute(signUpId, LookupHash("237650000001"), "042399");
        byte[] second = digest.Compute(signUpId, LookupHash("237650000001"), "042399");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Another_code_gives_a_different_digest_for_the_same_sign_up()
    {
        SignUpId signUpId = SignUpId.New();
        byte[] first = digest.Compute(signUpId, LookupHash("237650000001"), "042399");
        byte[] second = digest.Compute(signUpId, LookupHash("237650000001"), "042400");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Another_sign_up_gives_a_different_digest_for_the_same_code()
    {
        byte[] first = digest.Compute(SignUpId.New(), LookupHash("237650000001"), "042399");
        byte[] second = digest.Compute(SignUpId.New(), LookupHash("237650000001"), "042399");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Another_phone_gives_a_different_digest_for_the_same_code()
    {
        SignUpId signUpId = SignUpId.New();
        byte[] first = digest.Compute(signUpId, LookupHash("237650000001"), "042399");
        byte[] second = digest.Compute(signUpId, LookupHash("237660000001"), "042399");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_digest_is_always_32_bytes()
    {
        byte[] value = digest.Compute(SignUpId.New(), LookupHash("237650000001"), "042399");

        Assert.Equal(32, value.Length);
    }

    private static LookupHash LookupHash(string phone) => new(Encoding.UTF8.GetBytes(phone));

    private static VerificationCodeDigest Digest(string keyBase64) =>
        new(Options.Create(new SessionsOptions { VerificationCodeKeyBase64 = keyBase64 }));
}
