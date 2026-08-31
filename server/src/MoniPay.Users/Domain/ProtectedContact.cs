namespace MoniPay.Users.Domain;

/// <summary>
/// A phone number or an email address as the database holds it: encrypted so it can be read
/// back, and hashed so it can be looked up and kept unique.
/// </summary>
internal readonly record struct ProtectedContact(Ciphertext Ciphertext, LookupHash Hash);
