namespace MoniPay.Notifications.Domain;

internal static class RetrySchedule
{
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

    public static TimeSpan? NextDelay(string kind, int attempts)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);

        TimeSpan[] schedule = kind == VerificationCodeKind ? VerificationCode : Default;
        return attempts <= schedule.Length ? schedule[attempts - 1] : null;
    }
}
