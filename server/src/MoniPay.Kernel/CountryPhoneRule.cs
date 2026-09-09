namespace MoniPay.Kernel;

public readonly record struct CountryPhoneRule(string CallingCode, int LocalLength)
{
    public string CallingCode { get; } = ValidateCallingCode(CallingCode);

    public int LocalLength { get; } = ValidateLocalLength(LocalLength);

    private static string ValidateCallingCode(string callingCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(callingCode);
        foreach (char character in callingCode)
        {
            if (character is < '0' or > '9')
            {
                throw new ArgumentException("Calling code must be ASCII digits.", nameof(callingCode));
            }
        }

        return callingCode;
    }

    private static int ValidateLocalLength(int localLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(localLength);
        return localLength;
    }
}
