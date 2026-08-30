namespace MoniPay.Kernel;

/// <summary>Describes the calling code and local digit count accepted for one country.</summary>
public sealed record CountryPhoneRule(string CallingCode, int LocalLength);
