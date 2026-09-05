using Microsoft.Extensions.Options;

namespace MoniPay.Kernel.Security;

public static class KeyOptionsBuilderExtensions
{
    /// <summary>
    /// Refuses the options unless the selected value is a base64-encoded 32-byte key. The
    /// message names the configuration key, so a misconfigured host says which secret is missing.
    /// </summary>
    public static OptionsBuilder<TOptions> RequireKey<TOptions>(
        this OptionsBuilder<TOptions> builder,
        Func<TOptions, string> key,
        string configurationKey)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(key);

        return builder.Validate(
            options => Base64Key.IsValid(key(options)),
            $"{configurationKey} must hold a base64-encoded 32-byte key.");
    }
}
