using Microsoft.Extensions.Options;
using MoniPay.Sessions;
using MoniPay.Sessions.Security;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

/// <summary>
/// The generator's shape and randomness. Both distribution bands are many standard deviations
/// wide: the pooled two-percent band sits five sigmas out and the positional ten-percent band
/// ten, so neither can flake, yet every bias the issue targets — modulo bias, a stripped zero,
/// a position that never draws a digit — lands far outside one of them.
/// </summary>
public sealed class VerificationCodeGeneratorTests
{
    private const int SampleCount = 100_000;

    private static VerificationCodeGenerator WithLength(int length) =>
        new(Options.Create(new SessionsOptions { VerificationCodeLength = length }));

    private static readonly VerificationCodeGenerator Generator = WithLength(6);

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public void A_code_is_exactly_the_configured_number_of_digits(int length)
    {
        string code = WithLength(length).Next();

        Assert.Equal(length, code.Length);
        Assert.All(code.ToCharArray(), digit => Assert.InRange(digit, '0', '9'));
    }

    [Fact]
    public void A_code_starting_with_zero_keeps_its_full_length()
    {
        const int samples = 500;

        string[] codes = Enumerable.Range(0, samples).Select(_ => Generator.Next()).ToArray();

        // A leading zero in five hundred samples is a certainty; none would mean stripped zeros.
        Assert.Contains(codes, code => code[0] == '0');
        Assert.All(codes, code => Assert.Equal(6, code.Length));
    }

    [Fact]
    public void Every_position_draws_every_digit_and_the_pool_stays_uniform()
    {
        const int codeLength = 6;

        int[] pooledCounts = new int[10];
        int[][] positionCounts = new int[codeLength][];
        for (int position = 0; position < codeLength; position++)
        {
            positionCounts[position] = new int[10];
        }

        for (int sample = 0; sample < SampleCount; sample++)
        {
            string code = Generator.Next();
            for (int position = 0; position < codeLength; position++)
            {
                int digit = code[position] - '0';
                pooledCounts[digit] += 1;
                positionCounts[position][digit] += 1;
            }
        }

        long expectedPooled = SampleCount * codeLength / 10;
        double pooledTolerance = expectedPooled * 0.02;
        foreach (int count in pooledCounts)
        {
            Assert.InRange(count, expectedPooled - pooledTolerance, expectedPooled + pooledTolerance);
        }

        long expectedPositional = SampleCount / 10;
        double positionalTolerance = expectedPositional * 0.10;
        foreach (int[] counts in positionCounts)
        {
            Assert.All(counts, count => Assert.InRange(count, expectedPositional - positionalTolerance, expectedPositional + positionalTolerance));
        }
    }
}
