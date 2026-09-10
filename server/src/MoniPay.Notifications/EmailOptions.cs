using MoniPay.Kernel;

namespace MoniPay.Notifications;

internal sealed class EmailOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public bool HasWorkableEmail() =>
        IsHttpsBaseUrl(BaseUrl)
        && IsHeaderSafe(ApiKey)
        && EmailAddress.TryNormalize(FromAddress, out _);

    private static bool IsHttpsBaseUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps
            && uri.UserInfo.Length == 0
            && uri.Query.Length == 0
            && uri.Fragment.Length == 0;
    }

    private static bool IsHeaderSafe(string value) =>
        value.Length > 0 && value.All(character => character is > ' ' and < '\u007F');
}
