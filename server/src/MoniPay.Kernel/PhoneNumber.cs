namespace MoniPay.Kernel;

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
            if (!IsMatch(input, rule))
            {
                continue;
            }

            phone = new(input);
            failure = PhoneFailure.None;
            return true;
        }

        failure = PhoneFailure.CountryUnsupported;
        return false;
    }

    private static bool IsMatch(string input, CountryPhoneRule rule)
    {
        string callingCode = rule.CallingCode;
        if (string.IsNullOrEmpty(callingCode) || rule.LocalLength <= 0)
        {
            return false;
        }

        return input.StartsWith(callingCode, StringComparison.Ordinal)
            && input.Length - callingCode.Length == rule.LocalLength;
    }

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
