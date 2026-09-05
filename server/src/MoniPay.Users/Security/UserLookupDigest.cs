using Microsoft.Extensions.Options;
using MoniPay.Kernel.Security;

namespace MoniPay.Users.Security;

/// <summary>The kernel lookup digest under the Users personal-data key, for contact values.</summary>
internal sealed class UserLookupDigest(IOptions<UsersOptions> options)
    : LookupDigest(options.Value.PersonalDataKey, "MoniPay.Users:lookup"u8.ToArray());
