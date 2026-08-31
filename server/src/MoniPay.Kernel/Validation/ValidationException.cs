namespace MoniPay.Kernel.Validation;

/// <summary>An expected request validation failure with pointers into the JSON document.</summary>
public sealed class ValidationException : Exception
{
    /// <summary>The failures that caused the request to be rejected, frozen at construction.</summary>
    public IReadOnlyList<ValidationFailure> Failures { get; }

    public ValidationException(ValidationFailures failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        Failures = failures.ToArray();
    }
}
