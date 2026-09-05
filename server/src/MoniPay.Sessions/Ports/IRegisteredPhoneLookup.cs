using MoniPay.Kernel;

namespace MoniPay.Sessions.Ports;

/// <summary>
/// Answers whether a verified phone already belongs to a user; the host adapter asks Users. It
/// takes the normalized phone, not a hash, because the two modules hash under different keys.
/// </summary>
public interface IRegisteredPhoneLookup
{
    Task<UserId?> FindUserIdAsync(PhoneNumber phone, CancellationToken cancellationToken);
}
