using MoniPay.Kernel;

namespace MoniPay.Users.Domain;

internal sealed record ProtectedContact(Ciphertext Ciphertext, LookupHash Hash);
