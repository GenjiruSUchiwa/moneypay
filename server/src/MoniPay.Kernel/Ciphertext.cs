namespace MoniPay.Kernel;

/// <summary>
/// A personal value after encryption. The type exists so that a plaintext string cannot be
/// handed to an entity by mistake: only a protector produces one.
/// </summary>
public readonly record struct Ciphertext(string Value);
