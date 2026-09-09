using MoniPay.Kernel;

namespace MoniPay.Users.Features.Registration;

public sealed record RegisteredUser(UserId Id, bool Created);
