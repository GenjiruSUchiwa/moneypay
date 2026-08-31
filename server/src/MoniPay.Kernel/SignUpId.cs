namespace MoniPay.Kernel;

/// <summary>An immutable identifier for a sign-up workflow.</summary>
public readonly record struct SignUpId(Guid Value)
{
    /// <summary>Creates a time-ordered sign-up identifier.</summary>
    public static SignUpId New() => new(Guid.CreateVersion7());

    /// <summary>Parses a sign-up identifier from its standard GUID representation.</summary>
    public static SignUpId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(Guid.Parse(value));
    }

    /// <summary>Attempts to parse a sign-up identifier.</summary>
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

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}
