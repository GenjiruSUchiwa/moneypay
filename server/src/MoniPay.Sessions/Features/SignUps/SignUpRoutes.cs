namespace MoniPay.Sessions.Features.SignUps;

/// <summary>The paths the sign-up group serves, declared once so the endpoints, the links and the tests cannot drift apart.</summary>
internal static class SignUpRoutes
{
    /// <summary>The prefix every sign-up route hangs off.</summary>
    public const string Group = "/signups";

    /// <summary>
    /// Starting a sign-up: <c>POST /signups</c>. Empty, not "/", so the served path stays
    /// <c>/signups</c>.
    /// </summary>
    public const string Start = "";

    /// <summary>One sign-up, addressed by its identifier.</summary>
    public const string ById = "/{signUpId:guid}";

    /// <summary>Asking for another verification code: <c>POST /signups/{id}/verification-code-deliveries</c>.</summary>
    public const string VerificationCodeDeliveries = "/{signUpId:guid}/verification-code-deliveries";

    /// <summary>Proving control of the phone: <c>POST /signups/{id}/phone-verifications</c>.</summary>
    public const string PhoneVerifications = "/{signUpId:guid}/phone-verifications";

    /// <summary>Completing a sign-up: <c>POST /signups/{id}/completions</c>.</summary>
    public const string Completions = "/{signUpId:guid}/completions";
}
