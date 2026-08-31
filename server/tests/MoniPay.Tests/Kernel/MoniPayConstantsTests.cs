using MoniPay.Kernel;
using Xunit;

namespace MoniPay.Tests.Kernel;

public sealed class MoniPayConstantsTests
{
    [Fact]
    public void Authorization_policy_names_are_stable()
    {
        Assert.Equal(nameof(MoniPayPolicies.Registration), MoniPayPolicies.Registration);
        Assert.Equal(nameof(MoniPayPolicies.AuthenticatedUser), MoniPayPolicies.AuthenticatedUser);
    }

    [Fact]
    public void JWT_claim_names_match_the_token_contract()
    {
        Assert.Equal("sub", MoniPayClaimTypes.Subject);
        Assert.Equal("sid", MoniPayClaimTypes.SessionId);
        Assert.Equal("jti", MoniPayClaimTypes.TokenId);
    }
}
