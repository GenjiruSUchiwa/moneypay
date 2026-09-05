using Microsoft.Extensions.Options;
using MoniPay.Kernel.Security;

namespace MoniPay.Sessions.Security;

/// <summary>The kernel protector under the Sessions personal-data key, for the sign-up phone.</summary>
internal sealed class SignUpPersonalDataProtector(IOptions<SessionsOptions> options)
    : PersonalDataProtector(options.Value.PersonalDataKey);
