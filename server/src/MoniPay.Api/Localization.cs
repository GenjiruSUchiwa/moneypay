using System.Globalization;
using Microsoft.AspNetCore.Localization;
using MoniPay.Kernel;

namespace MoniPay.Api;

/// <summary>
/// The languages MoniPay answers in. French is the default because the product's users read
/// French; English is what a module's neutral .resx provides. fr-CM is listed so a Cameroonian
/// client's own header matches exactly and falls back to the fr resources.
/// </summary>
public static class Localization
{
    /// <summary>The default when a request states no preference.</summary>
    public static string DefaultCulture => Locale.Default.Value;

    public static CultureInfo[] SupportedCultures() =>
        [.. Locale.SupportedTags.Select(tag => new CultureInfo(tag))];
}

/// <summary>Conventions every module follows when it ships translated strings.</summary>
public static class LocalizationDefaults
{
    /// <summary>
    /// The folder a module keeps its .resx files in, relative to the project root. It is part
    /// of the resource base name the localizer computes, so a module that files them elsewhere
    /// gets silently untranslated strings.
    /// </summary>
    public const string ResourcesPath = "Resources";
}

public static class LocalizationExtensions
{
    /// <summary>
    /// Each module keeps its own strings under its own Resources/ folder; this only says where
    /// to look, and which languages the host will answer in.
    /// </summary>
    public static IServiceCollection AddMoniPayLocalization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLocalization(options => options.ResourcesPath = LocalizationDefaults.ResourcesPath);
        services.Configure<RequestLocalizationOptions>(options =>
        {
            CultureInfo[] supported = Localization.SupportedCultures();

            options.DefaultRequestCulture = new RequestCulture(Localization.DefaultCulture);
            options.SupportedCultures = supported;          // formats numbers and dates
            options.SupportedUICultures = supported;        // picks the .resx
            // Accept-Language only. A query string or a cookie deciding the language of a money
            // refusal is a surface we have no reason to expose.
            options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
        });

        return services;
    }
}
