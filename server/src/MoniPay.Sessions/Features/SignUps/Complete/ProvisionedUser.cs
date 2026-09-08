using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Complete;

/// <summary>The outcome of provisioning: the user's identity, and whether this call created it.</summary>
public sealed record ProvisionedUser(UserId Id, bool Created);
