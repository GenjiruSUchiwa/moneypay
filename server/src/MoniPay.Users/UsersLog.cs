using Microsoft.Extensions.Logging;
using MoniPay.Kernel;

namespace MoniPay.Users;

/// <summary>
/// The module's log events. A failed optional welcome carries only the user identifier and fixed
/// metadata: never the recipient, the name, the subject, the body, or the failure's own message.
/// </summary>
internal static partial class UsersLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "Welcome message enqueue for user {UserId} failed; registration continues without it")]
    public static partial void WelcomeMessageEnqueueFailed(ILogger logger, UserId userId);
}
