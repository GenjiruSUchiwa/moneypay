namespace MoniPay.Sessions.Features.SignUps;

/// <summary>The JSON:API resource types the sign-up group serves.</summary>
internal static class SignUpResourceTypes
{
    /// <summary>The sign-up workflow resource.</summary>
    public const string SignUps = "signups";

    /// <summary>The phone-verification command resource.</summary>
    public const string PhoneVerifications = "phone-verifications";

    /// <summary>The sign-up completion command resource.</summary>
    public const string SignUpCompletions = "signup-completions";
}
