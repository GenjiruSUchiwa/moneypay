namespace MoniPay.Kernel;

/// <summary>
/// The keyed hash a protected value is found by. The ciphertext cannot be indexed, so this is
/// the column that carries the unique constraint. Two hashes are only ever compared by the
/// database, so the type defines no equality of its own.
/// </summary>
public readonly struct LookupHash(byte[] value)
{
    public byte[] Value { get; } = value;
}
