namespace MoniPay.Kernel;

public readonly struct LookupHash(byte[] value)
{
    public byte[] Value { get; } = value;
}
