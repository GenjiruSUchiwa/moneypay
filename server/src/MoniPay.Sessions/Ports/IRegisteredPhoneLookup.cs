using MoniPay.Kernel;

namespace MoniPay.Sessions.Ports;

public interface IRegisteredPhoneLookup
{
    Task<UserId?> FindUserIdAsync(PhoneNumber phone, CancellationToken cancellationToken);
}
