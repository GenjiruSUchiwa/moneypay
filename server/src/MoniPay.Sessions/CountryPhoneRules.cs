using MoniPay.Kernel;

namespace MoniPay.Sessions;

internal static class CountryPhoneRules
{
    public static readonly CountryPhoneRule Cameroon = new("237", 9);

    public static readonly CountryPhoneRule IvoryCoast = new("225", 10);

    public static readonly CountryPhoneRule Senegal = new("221", 9);

    public static readonly CountryPhoneRule Gabon = new("241", 8);

    public static readonly CountryPhoneRule DrCongo = new("243", 9);

    public static readonly CountryPhoneRule Benin = new("229", 8);
}
