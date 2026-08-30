using MoniPay.Kernel;
using Xunit;

namespace MoniPay.Tests.Kernel;

public sealed class EmailAddressTests
{
    [Fact]
    public void An_email_is_trimmed_and_its_domain_is_lowercase_while_the_local_part_is_preserved()
    {
        bool normalized = EmailAddress.TryNormalize("  Foo@Example.CM  ", out EmailAddress email);

        Assert.True(normalized);
        Assert.Equal("Foo@example.cm", email.Value);
        Assert.Equal("foo@example.cm", email.LookupValue);
    }

    [Fact]
    public void Equivalent_email_casing_produces_one_lookup_value()
    {
        bool firstNormalized = EmailAddress.TryNormalize("Foo@Example.CM", out EmailAddress first);
        bool secondNormalized = EmailAddress.TryNormalize(" foo@example.cm ", out EmailAddress second);

        Assert.True(firstNormalized);
        Assert.True(secondNormalized);
        Assert.Equal(first.LookupValue, second.LookupValue);
    }

    [Theory]
    [InlineData("foo.example.com")]
    [InlineData("foo@@example.com")]
    [InlineData("@example.com")]
    [InlineData("foo@")]
    public void An_email_without_exactly_one_nonempty_at_sign_is_rejected(string input)
    {
        bool normalized = EmailAddress.TryNormalize(input, out _);

        Assert.False(normalized);
    }

    [Theory]
    [InlineData("foo@example")]
    [InlineData("foo@.com")]
    [InlineData("foo@example.")]
    public void An_email_without_a_domain_dot_between_domain_labels_is_rejected(string input)
    {
        bool normalized = EmailAddress.TryNormalize(input, out _);

        Assert.False(normalized);
    }

    [Fact]
    public void An_email_with_a_non_ascii_local_part_is_rejected()
    {
        bool normalized = EmailAddress.TryNormalize("élan@example.com", out _);

        Assert.False(normalized);
    }

    [Fact]
    public void An_email_longer_than_254_characters_is_rejected()
    {
        string input = new string('a', 243) + "@example.com";

        bool normalized = EmailAddress.TryNormalize(input, out _);

        Assert.False(normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void A_missing_email_value_is_rejected(string? input)
    {
        bool normalized = EmailAddress.TryNormalize(input, out _);

        Assert.False(normalized);
    }
}
