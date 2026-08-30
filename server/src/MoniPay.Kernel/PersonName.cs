using System.Globalization;
using System.Text;

namespace MoniPay.Kernel;

/// <summary>An immutable normalized person's name.</summary>
public readonly record struct PersonName(string Value)
{
    /// <summary>Trims, collapses whitespace, and validates a person's name.</summary>
    public static bool TryNormalize(string? input, out PersonName name)
    {
        name = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string collapsed = string.Join(
            ' ',
            input.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        int characterCount = 0;
        bool hasLetter = false;
        foreach (Rune rune in collapsed.EnumerateRunes())
        {
            if (rune.Value != ' ')
            {
                if (!IsAllowed(rune))
                {
                    return false;
                }

                hasLetter |= Rune.IsLetter(rune);
            }

            characterCount++;
            if (characterCount > 64)
            {
                return false;
            }
        }

        if (!hasLetter)
        {
            return false;
        }

        name = new(collapsed);
        return true;
    }

    /// <summary>Reports whether a person's name can be normalized.</summary>
    public static bool IsValid(string? input) => TryNormalize(input, out _);

    private static bool IsAllowed(Rune rune) =>
        Rune.IsLetter(rune)
        || rune.Value is '\'' or '\u2019' or '-'
        || Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark;
}
