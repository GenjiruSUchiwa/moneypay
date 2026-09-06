namespace MoniPay.Notifications.Domain;

/// <summary>
/// How long a retryable failure waits before the next attempt. A verification code lives five
/// minutes, so its schedule is short — a longer one would deliver a dead code.
/// </summary>
internal static class RetrySchedule
{
    /// <summary>The one kind with its own schedule; any other kind follows the defaults.</summary>
    public const string VerificationCodeKind = "VerificationCode";

    private static readonly TimeSpan[] VerificationCode =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
    ];

    private static readonly TimeSpan[] Default =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
    ];

    /// <summary>
    /// The delay before the next attempt after <paramref name="attempts"/> attempts have been
    /// made, or <c>null</c> when the schedule is exhausted and the message must end Failed.
    /// </summary>
    public static TimeSpan? NextDelay(string kind, int attempts)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);

        TimeSpan[] schedule = kind == VerificationCodeKind ? VerificationCode : Default;
        return attempts <= schedule.Length ? schedule[attempts - 1] : null;
    }
}
