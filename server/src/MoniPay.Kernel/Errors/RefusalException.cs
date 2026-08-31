namespace MoniPay.Kernel.Errors;

/// <summary>
/// An expected domain refusal. The host resolves the problem type to localized text; this exception
/// never carries user-facing prose.
/// </summary>
public sealed class RefusalException : Exception
{
    /// <summary>The problem type, which binds the stable code to its HTTP status.</summary>
    public ProblemType Type { get; }

    /// <summary>The delay before a retry, when the backend knows it.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>JSON Pointers to related request members, when applicable.</summary>
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
