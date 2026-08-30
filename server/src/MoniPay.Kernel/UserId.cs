namespace MoniPay.Kernel;

/// <summary>An immutable identifier for a user.</summary>
public readonly record struct UserId(Guid Value)
{
    /// <summary>Creates a time-ordered user identifier.</summary>
    public static UserId New() => new(Guid.CreateVersion7());

    /// <summary>Parses a user identifier from its standard GUID representation.</summary>
    public static UserId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(Guid.Parse(value));
    }

    /// <summary>Attempts to parse a user identifier.</summary>
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

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}
