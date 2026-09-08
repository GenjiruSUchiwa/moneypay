using Microsoft.Extensions.Localization;
using MoniPay.Kernel.Errors;
using MoniPay.Sessions;
using MoniPay.Users;

namespace MoniPay.Api.Errors;

/// <summary>
/// Resolves the localized title and detail of a stable problem code in the request culture. A code
/// names its owner explicitly: a prefix cannot tell <c>phone-already-registered</c> apart from the
/// module that produced it, and a module owns its own translations. An unknown code or a missing
/// resource falls back to the code itself, so a gap never throws a second exception.
/// </summary>
internal sealed class MoniPayProblemText(IStringLocalizerFactory factory)
{
    private const string DetailSuffix = "-detail";

    private static readonly IReadOnlyDictionary<string, Type> ModuleOwners =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            // Sessions owns the sign-up and session refusals.
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

            // Users owns the registration uniqueness refusals.
            [MoniPayErrorTypes.PhoneAlreadyRegistered.Code] = typeof(UserMessages),
            [MoniPayErrorTypes.EmailAlreadyRegistered.Code] = typeof(UserMessages),
        };

    /// <summary>The localized title, or the code itself when no resource exists.</summary>
    public string Title(string code) => Lookup(code, code) ?? code;

    /// <summary>The localized detail, or <c>null</c> when the code carries none.</summary>
    public string? Detail(string code) => Lookup(code, code + DetailSuffix);

    /// <summary>The localized text of a validation failure, or the code itself when none exists.</summary>
    public string FailureDetail(string code) => Lookup(code, code) ?? code;

    private string? Lookup(string code, string key)
    {
        Type owner = ModuleOwners.TryGetValue(code, out Type? module) ? module : typeof(ProblemMessages);
        LocalizedString value = factory.Create(owner)[key];

        return value.ResourceNotFound ? null : value.Value;
    }
}
