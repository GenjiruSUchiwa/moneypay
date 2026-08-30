namespace MoniPay.Kernel;

/// <summary>Explains why a phone number could not be normalized.</summary>
public enum PhoneFailure
{
    /// <summary>The number has not failed validation.</summary>
    None,

    /// <summary>The number is not a digits-only value between 8 and 15 characters.</summary>
    Format,

    /// <summary>The calling code or local digit count is not supported.</summary>
    CountryUnsupported,
}

/// <summary>An immutable digits-only E.164 phone number.</summary>
public readonly record struct PhoneNumber(string Value)
{
    /// <summary>Normalizes and validates a phone number against the configured country rules.</summary>
    public static bool TryNormalize(
        string? input,
        IReadOnlyCollection<CountryPhoneRule> rules,
        out PhoneNumber phone,
        out PhoneFailure failure)
    {
        phone = default;
        failure = PhoneFailure.Format;
        ArgumentNullException.ThrowIfNull(rules);

        if (input is null || input.Length is < 8 or > 15 || !ContainsOnlyAsciiDigits(input))
        {
            return false;
        }

        foreach (CountryPhoneRule rule in rules)
        {
            if (input.StartsWith(rule.CallingCode, StringComparison.Ordinal)
                && input.Length - rule.CallingCode.Length == rule.LocalLength)
            {
                phone = new(input);
                failure = PhoneFailure.None;
                return true;
            }
        }

        failure = PhoneFailure.CountryUnsupported;
        return false;
    }

    /// <summary>Reports whether a phone number satisfies the configured country rules.</summary>
    public static bool IsValid(string? input, IReadOnlyCollection<CountryPhoneRule> rules) =>
        TryNormalize(input, rules, out _, out _);

    private static bool ContainsOnlyAsciiDigits(string value)
    {
        foreach (char character in value)
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }
}
