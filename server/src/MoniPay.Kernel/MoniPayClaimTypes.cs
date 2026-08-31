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
}
