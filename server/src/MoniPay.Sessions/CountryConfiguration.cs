using MoniPay.Kernel;

namespace MoniPay.Sessions;

internal sealed class CountryConfiguration
{
    public string CallingCode { get; set; } = string.Empty;

    public int LocalLength { get; set; }

    public bool IsWellFormed => CountryPhoneRule.IsWellFormed(CallingCode, LocalLength);

    public CountryPhoneRule ToRule() => new(CallingCode, LocalLength);
}
