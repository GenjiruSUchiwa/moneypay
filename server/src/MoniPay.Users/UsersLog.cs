using Microsoft.Extensions.Logging;
using MoniPay.Kernel;

namespace MoniPay.Users;

internal static partial class UsersLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "Welcome message enqueue for user {UserId} failed; registration continues without it")]
    public static partial void WelcomeMessageEnqueueFailed(ILogger logger, UserId userId);
}
