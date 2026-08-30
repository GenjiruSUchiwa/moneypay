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
        if (input is null)
        {
            return false;
        }

        string trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        StringBuilder normalized = new(trimmed.Length);
        bool pendingSpace = false;
        int characterCount = 0;

        for (int index = 0; index < trimmed.Length; index++)
        {
            if (!TryAppendCharacter(
                    trimmed,
                    ref index,
                    normalized,
                    ref pendingSpace,
                    ref characterCount))
            {
                return false;
            }
        }

        if (normalized.Length == 0)
        {
            return false;
        }

        name = new(normalized.ToString());
        return true;
    }

    /// <summary>Reports whether a person's name can be normalized.</summary>
    public static bool IsValid(string? input) => TryNormalize(input, out _);

    private static bool TryAppendCharacter(
        string value,
        ref int index,
        StringBuilder normalized,
        ref bool pendingSpace,
        ref int characterCount)
    {
        char character = value[index];
        if (char.IsWhiteSpace(character))
        {
            pendingSpace = true;
            return true;
        }

        if (!IsAllowedCharacter(value, index, character))
        {
            return false;
        }

        if (pendingSpace && normalized.Length > 0)
        {
            normalized.Append(' ');
            characterCount++;
        }

        pendingSpace = false;
        normalized.Append(character);
        characterCount++;

        if (char.IsHighSurrogate(character))
        {
            if (!HasFollowingLowSurrogate(value, index))
            {
                return false;
            }

            normalized.Append(value[++index]);
        }

        return characterCount <= 64;
    }

    private static bool HasFollowingLowSurrogate(string value, int index) =>
        index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]);

    private static bool IsAllowedCharacter(string value, int index, char character)
    {
        if (character is '\'' or '\u2019' or '-')
        {
            return true;
        }

        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(value, index);
        return category is UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter
            or UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark;
    }
}
