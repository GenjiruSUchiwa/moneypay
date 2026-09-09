namespace MoniPay.Sessions.Features.Sessions;

/// <summary>The paths the session group serves, declared once so the endpoints, the links and the tests cannot drift apart.</summary>
internal static class SessionRoutes
{
    /// <summary>The prefix every session route hangs off.</summary>
    public const string Group = "/sessions";

    /// <summary>The session of the calling credential: <c>/sessions/current</c>.</summary>
    public const string Current = "/current";

    /// <summary>
    /// Rotating a session: <c>POST /session-refreshes</c>. It hangs off the root, not off
    /// <see cref="Group"/>: the refresh credential is the only thing that authorizes it, and a
    /// prefix would imply an anonymous <c>/sessions/session-refreshes</c> route.
    /// </summary>
    public const string Refreshes = "/session-refreshes";
}
