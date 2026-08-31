using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using MoniPay.Tests.Support;
using MoniPay.Users;
using MoniPay.Users.Domain;
using MoniPay.Users.Security;
using Xunit;

namespace MoniPay.Tests.Users.Registration;

public sealed class PersonalDataProtectionTests
{
    [Theory]
    [InlineData("Aristide")]
    [InlineData("237650000001")]
    [InlineData("user@example.com")]
    [InlineData("Ngo Nyobé")]
    public void Protection_round_trips_the_value(string plaintext)
    {
        UserPersonalDataProtector protector = Protector(TestKeys.UsersPersonalData);

        Ciphertext ciphertext = protector.Protect(plaintext);

        Assert.Equal(plaintext, protector.Unprotect(ciphertext));
        Assert.DoesNotContain(plaintext, ciphertext.Value);
    }

    [Fact]
    public void A_ciphertext_produced_with_another_key_fails_to_unprotect()
    {
        Ciphertext ciphertext = Protector(TestKeys.SessionsPersonalData).Protect("Aristide");

        Assert.ThrowsAny<CryptographicException>(() => Protector(TestKeys.UsersPersonalData).Unprotect(ciphertext));
    }

    [Fact]
    public void Protecting_the_same_value_twice_produces_different_ciphertexts()
    {
        UserPersonalDataProtector protector = Protector(TestKeys.UsersPersonalData);

        Assert.NotEqual(protector.Protect("Aristide"), protector.Protect("Aristide"));
    }

    [Fact]
    public void The_ciphertext_carries_the_key_version_prefix()
    {
        Ciphertext ciphertext = Protector(TestKeys.UsersPersonalData).Protect("Aristide");

        // A re-encryption migration finds rows by this byte; changing it is a migration, not an edit.
        Assert.Equal(1, Convert.FromBase64String(ciphertext.Value)[0]);
    }

    [Fact]
    public void The_lookup_digest_is_deterministic_per_key_and_value()
    {
        UserLookupDigest digest = Digest(TestKeys.UsersPersonalData);

        Assert.Equal(digest.Compute("user@example.com").Value, digest.Compute("user@example.com").Value);
        Assert.NotEqual(digest.Compute("user@example.com").Value, digest.Compute("other@example.com").Value);
        Assert.NotEqual(
            digest.Compute("user@example.com").Value,
            Digest(TestKeys.SessionsPersonalData).Compute("user@example.com").Value);
    }

    private static UserPersonalDataProtector Protector(string keyBase64) =>
        new(Options.Create(new UsersOptions { PersonalDataKeyBase64 = keyBase64 }));

    private static UserLookupDigest Digest(string keyBase64) =>
        new(Options.Create(new UsersOptions { PersonalDataKeyBase64 = keyBase64 }));
}
