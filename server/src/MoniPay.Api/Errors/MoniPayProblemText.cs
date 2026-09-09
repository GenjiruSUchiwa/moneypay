using Microsoft.Extensions.Localization;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Validation;
using MoniPay.Sessions;
using MoniPay.Users;

namespace MoniPay.Api.Errors;

internal sealed class MoniPayProblemText(IStringLocalizerFactory factory)
{
    private const string DetailSuffix = "-detail";

    private static readonly IReadOnlyDictionary<string, Type> ModuleOwners =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            [MoniPayErrorTypes.SignUpStateInvalid.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.VerificationCodeInvalid.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.VerificationCodeExpired.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.SignUpExpired.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.SignUpAttemptLimit.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.SignUpResendLimit.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.SignUpResendTooSoon.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.SignUpTokenInvalid.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.RegistrationTokenInvalid.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.SessionInvalid.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.RefreshTokenReused.Code] = typeof(SessionMessages),
            [MoniPayErrorTypes.VerificationDeliveryUnavailable.Code] = typeof(SessionMessages),

            [ValidationCodes.PhoneFormatInvalid] = typeof(SessionMessages),
            [ValidationCodes.PhoneCountryUnsupported] = typeof(SessionMessages),
            [ValidationCodes.LegalVersionOutdated] = typeof(SessionMessages),
            [ValidationCodes.VerificationCodeFormatInvalid] = typeof(SessionMessages),
            [ValidationCodes.PersonNameInvalid] = typeof(SessionMessages),
            [ValidationCodes.EmailInvalid] = typeof(SessionMessages),
            [ValidationCodes.DeviceIdRequired] = typeof(SessionMessages),
            [ValidationCodes.RefreshTokenFormatInvalid] = typeof(SessionMessages),

            [MoniPayErrorTypes.PhoneAlreadyRegistered.Code] = typeof(UserMessages),
            [MoniPayErrorTypes.EmailAlreadyRegistered.Code] = typeof(UserMessages),
        };

    public string Title(string code) => Lookup(code, code) ?? code;

    public string? Detail(string code) => Lookup(code, code + DetailSuffix);

    public string FailureDetail(string code) => Lookup(code, code) ?? code;

    private string? Lookup(string code, string key)
    {
        Type owner = ModuleOwners.TryGetValue(code, out Type? module) ? module : typeof(ProblemMessages);
        LocalizedString value = factory.Create(owner)[key];

        return value.ResourceNotFound ? null : value.Value;
    }
}
