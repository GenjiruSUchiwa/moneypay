using Microsoft.Extensions.Options;
using MoniPay.Kernel.Security;

namespace MoniPay.Users.Security;

/// <summary>The kernel protector under the Users personal-data key: first name, last name, phone and email.</summary>
internal sealed class UserPersonalDataProtector(IOptions<UsersOptions> options)
    : PersonalDataProtector(options.Value.PersonalDataKey);
