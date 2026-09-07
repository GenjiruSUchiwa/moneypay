namespace MoniPay.Sessions.Security;

/// <summary>
/// The names of the authentication schemes the module owns. A scheme name is also the value of
/// <c>WWW-Authenticate</c> on its challenge, and the prefix a client must use in the
/// <c>Authorization</c> header. The access-token scheme is the framework's own
/// <c>Bearer</c> (<c>JwtBearerDefaults.AuthenticationScheme</c>).
/// </summary>
public static class SessionsSchemes
{
    /// <summary>The scheme of a sign-up's first credential, issued at start.</summary>
    public const string SignUp = nameof(SignUp);

    /// <summary>The scheme of the credential issued once the phone is verified.</summary>
    public const string Registration = nameof(Registration);
}
