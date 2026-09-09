namespace MoniPay.Users.Features.CurrentUser;

/// <summary>The paths the user group serves, declared once so the endpoints, the links and the tests cannot drift apart.</summary>
internal static class UserRoutes
{
    /// <summary>The prefix every user route hangs off.</summary>
    public const string Group = "/users";

    /// <summary>The user of the calling credential: <c>/users/me</c>.</summary>
    public const string Me = "/me";
}
