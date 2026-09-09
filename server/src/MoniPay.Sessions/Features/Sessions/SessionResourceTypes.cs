namespace MoniPay.Sessions.Features.Sessions;

/// <summary>The JSON:API resource types the session group serves and names.</summary>
internal static class SessionResourceTypes
{
    /// <summary>The session resource: the credentials form, the read form, and the completion response.</summary>
    public const string Sessions = "sessions";

    /// <summary>The refresh command resource.</summary>
    public const string SessionRefreshes = "session-refreshes";

    /// <summary>
    /// The related user resource of a session's <c>user</c> relationship. The Users module owns
    /// this type and its <c>/users/me</c> route; the relationship is the only thing Sessions names.
    /// </summary>
    public const string Users = "users";
}
