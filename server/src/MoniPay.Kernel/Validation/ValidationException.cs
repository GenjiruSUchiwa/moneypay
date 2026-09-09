namespace MoniPay.Kernel.Validation;

public sealed class ValidationException : Exception
{
    public IReadOnlyList<ValidationFailure> Failures { get; }

    public ValidationException(ValidationFailures failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        Failures = failures.ToArray();
    }
}
