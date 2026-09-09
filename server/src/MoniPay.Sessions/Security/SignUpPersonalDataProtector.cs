using Microsoft.Extensions.Options;
using MoniPay.Kernel.Security;

namespace MoniPay.Sessions.Security;

internal sealed class SignUpPersonalDataProtector(IOptions<SessionsOptions> options)
    : PersonalDataProtector(options.Value.PersonalDataKey);
