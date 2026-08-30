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
