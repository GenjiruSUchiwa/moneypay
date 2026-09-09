using MoniPay.Kernel;
using MoniPay.Sessions.Ports;
using MoniPay.Users.Features.Registration;

namespace MoniPay.Api.Composition;

internal sealed class RegisteredPhoneLookupAdapter(PhoneRegistrationLookup phones) : IRegisteredPhoneLookup
{
    public Task<UserId?> FindUserIdAsync(PhoneNumber phone, CancellationToken cancellationToken) =>
        phones.FindUserIdAsync(phone, cancellationToken);
}
