using MoniPay.Kernel;
using Xunit;

namespace MoniPay.Tests.Kernel;

public sealed class PersonNameTests
{
    [Fact]
    public void A_person_name_is_trimmed_and_internal_whitespace_is_collapsed()
    {
        bool normalized = PersonName.TryNormalize("  Jean\t  Luc\n", out PersonName name);

        Assert.True(normalized);
        Assert.Equal("Jean Luc", name.Value);
    }

    [Fact]
    public void A_name_with_unicode_letters_and_marks_is_accepted()
    {
        const string input = "Jose\u0301";

        bool normalized = PersonName.TryNormalize(input, out PersonName name);

        Assert.True(normalized);
        Assert.Equal(input, name.Value);
    }

    [Fact]
    public void Apostrophes_and_hyphens_are_accepted_in_a_name()
    {
        bool normalized = PersonName.TryNormalize(" O'Connor-Smith ", out PersonName name);

        Assert.True(normalized);
        Assert.Equal("O'Connor-Smith", name.Value);
    }

    [Theory]
    [InlineData("Jean2")]
    [InlineData("Jean.")]
    [InlineData("Jean!")]
    [InlineData("---")]
    [InlineData("'''")]
    [InlineData("\u0301")]
    public void Digits_punctuation_and_letterless_names_are_rejected(string input)
    {
        bool normalized = PersonName.TryNormalize(input, out _);

        Assert.False(normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \t\n")]
    public void An_empty_or_whitespace_only_name_is_rejected(string? input)
    {
        bool normalized = PersonName.TryNormalize(input, out _);

        Assert.False(normalized);
    }

    [Fact]
    public void A_name_of_64_letters_is_accepted()
    {
        string input = new('A', 64);

        bool normalized = PersonName.TryNormalize(input, out PersonName name);

        Assert.True(normalized);
        Assert.Equal(input, name.Value);
    }

    [Fact]
    public void A_name_longer_than_64_characters_is_rejected()
    {
        string input = new('A', 65);

        bool normalized = PersonName.TryNormalize(input, out _);

        Assert.False(normalized);
    }

    [Fact]
    public void A_supplementary_plane_letter_is_accepted()
    {
        string input = char.ConvertFromUtf32(0x1E900);

        bool normalized = PersonName.TryNormalize(input, out PersonName name);

        Assert.True(normalized);
        Assert.Equal(input, name.Value);
    }

    [Fact]
    public void IsValid_matches_the_name_normalization_result()
    {
        Assert.True(PersonName.IsValid("Jean-Marie"));
        Assert.False(PersonName.IsValid("Jean-Marie2"));
    }
}
