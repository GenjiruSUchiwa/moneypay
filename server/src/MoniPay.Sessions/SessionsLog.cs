using Microsoft.Extensions.Logging;

namespace MoniPay.Sessions;

/// <summary>The module's log events. None carries a phone, a code or a token.</summary>
internal static partial class SessionsLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Expired-credential cleanup deleted {Deleted} rows older than {Cutoff}")]
    public static partial void CredentialsCleaned(ILogger logger, int deleted, DateTimeOffset cutoff);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "The expired-credential cleanup sweep failed")]
    public static partial void CleanupFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "The refresh token was refused: {Reason}")]
    public static partial void RefreshRefused(ILogger logger, string reason);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning,
        Message = "Refresh-token reuse revoked the active sessions of family {TokenFamilyId}")]
    public static partial void FamilyRevoked(ILogger logger, Guid tokenFamilyId);
}
