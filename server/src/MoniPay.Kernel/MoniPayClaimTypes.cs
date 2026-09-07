namespace MoniPay.Kernel;

/// <summary>JWT claim names used by MoniPay authentication handlers.</summary>
public static class MoniPayClaimTypes
{
    /// <summary>The subject claim containing the user identifier.</summary>
    public const string Subject = "sub";

    /// <summary>The session identifier claim.</summary>
    public const string SessionId = "sid";

    /// <summary>The token identifier claim.</summary>
    public const string TokenId = "jti";

    /// <summary>
    /// The sign-up identifier claim the workflow schemes issue and the routes name as their
    /// <c>signUpId</c> parameter: a workflow credential is bound to one sign-up.
    /// </summary>
    public const string SignUpId = "signUpId";
}
