namespace MoniPay.Kernel.Http;

/// <summary>
/// HTTP header names and authentication scheme names shared across modules.
/// Scheme values are the tokens that appear after <c>Authorization:</c>.
/// </summary>
public static class MoniPayHeaders
{
    /// <summary>The app version, for compatibility checks.</summary>
    public const string Client = "X-MoniPay-Client";

    /// <summary>The delay before a rate-limited or locked client may retry.</summary>
    public const string RetryAfter = "Retry-After";

    /// <summary>The sign-up workflow token scheme: <c>Authorization: SignUp &lt;token&gt;</c>.</summary>
    public const string SignUp = nameof(SignUp);

    /// <summary>The registration workflow token scheme: <c>Authorization: Registration &lt;token&gt;</c>.</summary>
    public const string Registration = nameof(Registration);

    /// <summary>The access-token scheme: <c>Authorization: Bearer &lt;jwt&gt;</c>.</summary>
    public const string Bearer = nameof(Bearer);
}
