using System.Globalization;

namespace MoniPay.Kernel;

/// <summary>
/// Runs a renderer under a stored locale's culture so the localizer picks that language, then
/// restores the ambient culture: the request or worker thread never inherits the message's.
/// </summary>
public static class LocaleCultureExtensions
{
    /// <summary>Invokes <paramref name="render"/> with <paramref name="locale"/> as the current culture.</summary>
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
