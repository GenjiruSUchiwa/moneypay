namespace MoniPay.Kernel;

public readonly record struct SignUpId(Guid Value)
{
    public static SignUpId New() => new(Guid.CreateVersion7());

    public static SignUpId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(Guid.Parse(value));
    }

    public static bool TryParse(string? value, out SignUpId signUpId)
    {
        if (Guid.TryParse(value, out Guid parsed))
        {
            signUpId = new(parsed);
            return true;
        }

        signUpId = default;
        return false;
    }

    public override string ToString() => Value.ToString("D");
}
