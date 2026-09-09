using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Security;

namespace MoniPay.Sessions.Security;

internal sealed class PhoneLookupDigest(IOptions<SessionsOptions> options)
    : LookupDigest(options.Value.PersonalDataKey, "MoniPay.Sessions:lookup"u8.ToArray())
{
    public LookupHash Compute(PhoneNumber phone) => Compute(phone.Value);
}
