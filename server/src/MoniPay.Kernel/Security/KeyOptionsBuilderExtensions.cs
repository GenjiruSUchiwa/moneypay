using Microsoft.Extensions.Options;

namespace MoniPay.Kernel.Security;

public static class KeyOptionsBuilderExtensions
{
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
