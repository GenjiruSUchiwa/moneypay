namespace MoniPay.Sessions.Features.Sessions;

internal static class SessionSummaries
{
    public const string CreateSessionRefresh = "Exchanges a refresh token for a new access and refresh credential pair.";

    public const string GetCurrentSession = "Returns the current session without its credentials.";

    public const string DeleteCurrentSession = "Revokes the current session.";
}
