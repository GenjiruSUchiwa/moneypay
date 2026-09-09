using System.Security.Cryptography;
using System.Text;
using MoniPay.Sessions.Security;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

public sealed class RefreshTokenFactoryTests
{
    private readonly RefreshTokenFactory factory = new();

    [Fact]
    public void A_token_is_43_base64url_characters()
    {
        (string raw, _) = factory.Create();

        Assert.Equal(RefreshTokenFactory.RawLength, raw.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", raw);
    }

    [Fact]
    public void The_digest_is_the_sha256_of_the_raw_value()
    {
        (string raw, byte[] digest) = factory.Create();

        Assert.Equal(32, digest.Length);
        byte[] expected = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        Assert.Equal(expected, digest);
    }

    [Fact]
    public void Ten_thousand_tokens_are_all_different()
    {
        HashSet<string> raws = [];

        for (int sample = 0; sample < 10_000; sample++)
        {
            raws.Add(factory.Create().Raw);
        }

        Assert.Equal(10_000, raws.Count);
    }
}
