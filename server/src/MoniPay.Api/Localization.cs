using System.Globalization;
using Microsoft.AspNetCore.Localization;
using MoniPay.Kernel;

namespace MoniPay.Api;

public static class Localization
{
    public static string DefaultCulture => Locale.Default.Value;

    public static CultureInfo[] SupportedCultures() =>
        [.. Locale.SupportedTags.Select(tag => new CultureInfo(tag))];
}

public static class LocalizationDefaults
{
    public const string ResourcesPath = "Resources";
}

public static class LocalizationExtensions
{
    public static IServiceCollection AddMoniPayLocalization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLocalization(options => options.ResourcesPath = LocalizationDefaults.ResourcesPath);
        services.Configure<RequestLocalizationOptions>(options =>
        {
            CultureInfo[] supported = Localization.SupportedCultures();

            options.DefaultRequestCulture = new RequestCulture(Localization.DefaultCulture);
            options.SupportedCultures = supported;
            options.SupportedUICultures = supported;
            options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
        });

        return services;
    }
}
