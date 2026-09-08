using Microsoft.Extensions.Logging;

namespace MoniPay.Api.Errors;

/// <summary>
/// The host's outcome events: one per mapped failure, never one per exception that produced no
/// response. None carries an exception message, a credential, or any request value.
/// </summary>
internal static partial class MoniPayErrorLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Validation refused {Route}: {Codes}")]
    public static partial void ValidationRefused(ILogger logger, string route, string codes);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Request refused {Route}: {Code}")]
    public static partial void RequestRefused(ILogger logger, string route, string code);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "Provider unavailable {Provider}: {ResultCode}")]
    public static partial void ProviderUnavailable(ILogger logger, string provider, string? resultCode);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning,
        Message = "Concurrency conflict {Route}: {Entity}")]
    public static partial void ConcurrencyConflict(ILogger logger, string route, string entity);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error,
        Message = "Unhandled exception {Route}")]
    public static partial void UnhandledException(ILogger logger, Exception exception, string route);
}
