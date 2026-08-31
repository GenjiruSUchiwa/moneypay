using System.Collections;

namespace MoniPay.Kernel.Validation;

/// <summary>Collects validation failures while a request's attributes are checked.</summary>
public sealed class ValidationFailures : IReadOnlyCollection<ValidationFailure>
{
    private readonly List<ValidationFailure> failures = [];

    /// <summary>Whether at least one validation failure has been collected.</summary>
    public bool Any() => failures.Count > 0;

    /// <summary>The number of validation failures collected so far.</summary>
    public int Count => failures.Count;

    /// <summary>Adds a failure when <paramref name="condition"/> is false.</summary>
    public void Require(bool condition, string pointer, string code)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        ArgumentException.ThrowIfNullOrEmpty(code);

        if (condition)
        {
            return;
        }

        failures.Add(new ValidationFailure(pointer, code));
    }

    /// <inheritdoc />
    public IEnumerator<ValidationFailure> GetEnumerator() => failures.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
