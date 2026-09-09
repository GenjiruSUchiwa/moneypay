using System.Collections;

namespace MoniPay.Kernel.Validation;

public sealed class ValidationFailures : IReadOnlyCollection<ValidationFailure>
{
    private readonly List<ValidationFailure> failures = [];

    public bool Any() => failures.Count > 0;

    public int Count => failures.Count;

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

    public IEnumerator<ValidationFailure> GetEnumerator() => failures.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
