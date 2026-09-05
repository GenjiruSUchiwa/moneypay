using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Security;

namespace MoniPay.Sessions.Security;

/// <summary>
/// The kernel lookup digest under the Sessions personal-data key. Users derives its own from its
/// own key, so a phone is never matched across modules by hash — only through a port on the
/// normalized <see cref="PhoneNumber"/>.
/// </summary>
internal sealed class PhoneLookupDigest(IOptions<SessionsOptions> options)
    : LookupDigest(options.Value.PersonalDataKey, "MoniPay.Sessions:lookup"u8.ToArray())
{
    public LookupHash Compute(PhoneNumber phone) => Compute(phone.Value);
}
