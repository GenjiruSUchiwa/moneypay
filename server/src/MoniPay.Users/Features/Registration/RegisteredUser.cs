using MoniPay.Kernel;

namespace MoniPay.Users.Features.Registration;

/// <summary>The outcome of a registration: the user's identity, and whether this call created it.</summary>
public sealed record RegisteredUser(UserId Id, bool Created);
