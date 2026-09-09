using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Security;
using MoniPay.Sessions;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

public sealed class PersonalDataProtectorTests
{
    [Fact]
    public void Protection_round_trips_the_phone_under_the_sessions_key()
    {
        SignUpPersonalDataProtector protector = new(WithKey(TestKeys.SessionsPersonalData));

        Assert.Equal("237650000001", protector.Unprotect(protector.Protect("237650000001")));
    }

    [Fact]
    public void The_lookup_digest_is_per_phone_and_per_module()
    {
        PhoneLookupDigest digest = new(WithKey(TestKeys.SessionsPersonalData));
        LookupDigest sameKeyOtherModule = new(
            Base64Key.Decode(TestKeys.SessionsPersonalData),
            "MoniPay.Users:lookup"u8.ToArray());

        Assert.Equal(
            digest.Compute(new PhoneNumber("237650000001")).Value,
            digest.Compute(new PhoneNumber("237650000001")).Value);
        Assert.NotEqual(
            digest.Compute(new PhoneNumber("237650000001")).Value,
            digest.Compute(new PhoneNumber("237660000001")).Value);
        Assert.NotEqual(
            digest.Compute(new PhoneNumber("237650000001")).Value,
            sameKeyOtherModule.Compute("237650000001").Value);
    }

    private static IOptions<SessionsOptions> WithKey(string keyBase64) =>
        Options.Create(new SessionsOptions { PersonalDataKeyBase64 = keyBase64 });
}
