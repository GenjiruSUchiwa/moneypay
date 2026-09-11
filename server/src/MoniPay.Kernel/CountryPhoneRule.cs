namespace MoniPay.Kernel;

public readonly record struct CountryPhoneRule(string CallingCode, int LocalLength)
{
    public string CallingCode { get; } = ValidateCallingCode(CallingCode);

    public int LocalLength { get; } = ValidateLocalLength(LocalLength);

    public static bool IsWellFormed(string? callingCode, int localLength) =>
        callingCode is { Length: > 0 } value
        && value.All(char.IsAsciiDigit)
        && localLength > 0;

    private static string ValidateCallingCode(string callingCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(callingCode);
        if (!callingCode.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Calling code must be ASCII digits.", nameof(callingCode));
        }

        return callingCode;
    }

    private static int ValidateLocalLength(int localLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(localLength);
        return localLength;
    }
}
