using Microsoft.Extensions.Options;
using MoniPay.Kernel.Security;

namespace MoniPay.Users.Security;

internal sealed class UserLookupDigest(IOptions<UsersOptions> options)
    : LookupDigest(options.Value.PersonalDataKey, "MoniPay.Users:lookup"u8.ToArray());
