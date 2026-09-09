namespace MoniPay.Kernel.Errors;

public sealed class RefusalException : Exception
{
    public ProblemType Type { get; }

    public TimeSpan? RetryAfter { get; }

    public IReadOnlyList<string>? Pointers { get; }

    public RefusalException(
        ProblemType type,
        TimeSpan? retryAfter = null,
        IReadOnlyList<string>? pointers = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (retryAfter is { } delay && delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryAfter), "The retry delay cannot be negative.");
        }

        Type = type;
        RetryAfter = retryAfter;
        Pointers = pointers?.ToArray();
    }
}
