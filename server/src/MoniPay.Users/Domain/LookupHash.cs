namespace MoniPay.Users.Domain;

/// <summary>
/// The keyed hash a contact value is found by. The ciphertext cannot be indexed, so this is the
/// column that carries the unique constraint. Equality is by reference on purpose: two hashes
/// are only ever compared by the database.
/// </summary>
internal readonly record struct LookupHash(byte[] Value);
