using Microsoft.Extensions.Options;
using MoniPay.Kernel.Security;

namespace MoniPay.Users.Security;

internal sealed class UserPersonalDataProtector(IOptions<UsersOptions> options)
    : PersonalDataProtector(options.Value.PersonalDataKey);
