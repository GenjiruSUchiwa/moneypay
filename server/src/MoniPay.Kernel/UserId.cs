namespace MoniPay.Kernel;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.CreateVersion7());

    public static UserId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(Guid.Parse(value));
    }

    public static bool TryParse(string? value, out UserId userId)
    {
        if (Guid.TryParse(value, out Guid parsed))
        {
            userId = new(parsed);
            return true;
        }

        userId = default;
        return false;
    }

    public override string ToString() => Value.ToString("D");
}
