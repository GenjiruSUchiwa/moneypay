namespace MoniPay.Users.Domain;

/// <summary>
/// A personal value after encryption. The type exists so that a plaintext string cannot be
/// handed to the entity by mistake: only the protector produces one.
/// </summary>
internal readonly record struct Ciphertext(string Value);
