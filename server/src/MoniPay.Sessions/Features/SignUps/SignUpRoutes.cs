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
}
