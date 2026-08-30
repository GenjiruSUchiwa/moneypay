using MoniPay.Kernel;
using Xunit;

namespace MoniPay.Tests.Kernel;

public sealed class PhoneNumberTests
{
    private static readonly CountryPhoneRule[] Rules =
    [
        new("237", 9),
        new("225", 10),
        new("221", 9),
        new("241", 8),
        new("243", 9),
        new("229", 8),
    ];

    [Theory]
    [InlineData("237", "612345678")]
    [InlineData("225", "0123456789")]
    [InlineData("221", "771234567")]
    [InlineData("241", "06123456")]
    [InlineData("243", "812345678")]
    [InlineData("229", "90123456")]
    public void A_number_for_each_supported_country_is_accepted(string callingCode, string localNumber)
    {
        string input = callingCode + localNumber;

        bool normalized = PhoneNumber.TryNormalize(input, Rules, out PhoneNumber phone, out PhoneFailure failure);

        Assert.True(normalized);
        Assert.Equal(input, phone.Value);
        Assert.Equal(PhoneFailure.None, failure);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("1234567890123456")]
    public void A_number_outside_the_eight_to_fifteen_digit_range_has_a_format_failure(string input)
    {
        bool normalized = PhoneNumber.TryNormalize(input, Rules, out _, out PhoneFailure failure);

        Assert.False(normalized);
        Assert.Equal(PhoneFailure.Format, failure);
    }

    [Fact]
    public void A_number_with_a_supported_prefix_and_wrong_local_length_has_a_country_failure()
    {
        bool normalized = PhoneNumber.TryNormalize("23761234567", Rules, out _, out PhoneFailure failure);

        Assert.False(normalized);
        Assert.Equal(PhoneFailure.CountryUnsupported, failure);
    }

    [Theory]
    [InlineData("999612345678")]
    [InlineData("238612345678")]
    public void An_unsupported_country_or_prefix_has_a_country_failure(string input)
    {
        bool normalized = PhoneNumber.TryNormalize(input, Rules, out _, out PhoneFailure failure);

        Assert.False(normalized);
        Assert.Equal(PhoneFailure.CountryUnsupported, failure);
    }

    [Theory]
    [InlineData("+237612345678")]
    [InlineData("237 612345678")]
    [InlineData("237\t612345678")]
    [InlineData("237٦12345678")]
    public void A_number_with_non_ascii_digits_or_formatting_has_a_format_failure(string input)
    {
        bool normalized = PhoneNumber.TryNormalize(input, Rules, out _, out PhoneFailure failure);

        Assert.False(normalized);
        Assert.Equal(PhoneFailure.Format, failure);
    }

    [Fact]
    public void IsValid_matches_the_normalization_result()
    {
        Assert.True(PhoneNumber.IsValid("237612345678", Rules));
        Assert.False(PhoneNumber.IsValid("23761234567", Rules));
    }
}
