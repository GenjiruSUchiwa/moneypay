using System.Globalization;

namespace MoniPay.Kernel;

public static class LocaleCultureExtensions
{
    public static T Invoke<T>(this Locale locale, Func<T> render)
    {
        ArgumentNullException.ThrowIfNull(render);

        CultureInfo culture = CultureInfo.GetCultureInfo(locale.Value);
        CultureInfo currentCulture = CultureInfo.CurrentCulture;
        CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return render();
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUICulture;
        }
    }
}
