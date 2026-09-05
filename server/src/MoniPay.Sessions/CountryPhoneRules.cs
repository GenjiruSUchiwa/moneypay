using MoniPay.Kernel;

namespace MoniPay.Sessions;

/// <summary>
/// The six markets a sign-up may originate from, the same six the iOS country list ships. Each
/// entry names its country so the options default never carries a raw calling code.
/// </summary>
internal static class CountryPhoneRules
{
    public static readonly CountryPhoneRule Cameroon = new("237", 9);

    public static readonly CountryPhoneRule IvoryCoast = new("225", 10);

    public static readonly CountryPhoneRule Senegal = new("221", 9);

    public static readonly CountryPhoneRule Gabon = new("241", 8);

    public static readonly CountryPhoneRule DrCongo = new("243", 9);

    public static readonly CountryPhoneRule Benin = new("229", 8);
}
