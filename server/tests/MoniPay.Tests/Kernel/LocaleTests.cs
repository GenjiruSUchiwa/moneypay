using MoniPay.Kernel;
using Xunit;

namespace MoniPay.Tests.Kernel;

public sealed class LocaleTests
{
    [Theory]
    [InlineData("fr-CM", "fr-CM")]
    [InlineData(" FR-cm ", "fr-CM")]
    [InlineData("fr", "fr")]
    [InlineData("EN", "en")]
    public void A_supported_tag_parses_to_its_canonical_form(string input, string expected)
    {
        Assert.True(Locale.TryParse(input, out Locale locale));
        Assert.Equal(expected, locale.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("de")]
    [InlineData("fr-FR")]
    public void An_unsupported_tag_is_refused(string? input)
    {
        Assert.False(Locale.TryParse(input, out _));
    }

    [Fact]
    public void The_default_is_french()
    {
        Assert.Equal(Locale.French, Locale.Default);
    }
}
