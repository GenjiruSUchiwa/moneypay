using MoniPay.Kernel;

namespace MoniPay.Users.Domain;

/// <summary>
/// A phone number or an email address as the database holds it: encrypted so it can be read
/// back, and hashed so it can be looked up and kept unique. Mapped as an owned type, so the
/// two columns live on the <c>users</c> table and the hash carries the unique index.
/// </summary>
internal sealed record ProtectedContact(Ciphertext Ciphertext, LookupHash Hash);
