using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Api.Errors;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Validation;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

public sealed class ProblemTextLocalizationTests(MoniPayApi api)
{
    public static TheoryData<string, string, string> ValidationCodes_ => new()
    {
        { ValidationCodes.PhoneFormatInvalid, Locale.EnglishTag, "The phone number format is invalid." },
        { ValidationCodes.PhoneFormatInvalid, Locale.FrenchTag, "Le format du numéro de téléphone est invalide." },
        { ValidationCodes.PhoneCountryUnsupported, Locale.EnglishTag, "The phone country is not supported." },
        { ValidationCodes.PhoneCountryUnsupported, Locale.FrenchTag, "Le pays du numéro de téléphone n'est pas pris en charge." },
        { ValidationCodes.LegalVersionOutdated, Locale.EnglishTag, "The accepted legal document version is not current." },
        { ValidationCodes.LegalVersionOutdated, Locale.FrenchTag, "La version acceptée du document juridique n'est plus à jour." },
        { ValidationCodes.VerificationCodeFormatInvalid, Locale.EnglishTag, "The verification code format is invalid." },
        { ValidationCodes.VerificationCodeFormatInvalid, Locale.FrenchTag, "Le format du code de vérification est invalide." },
        { ValidationCodes.PersonNameInvalid, Locale.EnglishTag, "The name is invalid." },
        { ValidationCodes.PersonNameInvalid, Locale.FrenchTag, "Le nom est invalide." },
        { ValidationCodes.EmailInvalid, Locale.EnglishTag, "The email address is invalid." },
        { ValidationCodes.EmailInvalid, Locale.FrenchTag, "L'adresse e-mail est invalide." },
        { ValidationCodes.DeviceIdRequired, Locale.EnglishTag, "The device identifier is required." },
        { ValidationCodes.DeviceIdRequired, Locale.FrenchTag, "L'identifiant de l'appareil est obligatoire." },
        { ValidationCodes.RefreshTokenFormatInvalid, Locale.EnglishTag, "The refresh token format is invalid." },
        { ValidationCodes.RefreshTokenFormatInvalid, Locale.FrenchTag, "Le format du jeton de rafraîchissement est invalide." },
    };

    public static TheoryData<string, string, string> RefusalTitles => new()
    {
        { MoniPayErrorTypes.SignUpStateInvalid.Code, Locale.EnglishTag, "The sign-up is not in a state that allows this action." },
        { MoniPayErrorTypes.SignUpStateInvalid.Code, Locale.FrenchTag, "L'inscription n'est pas dans un état qui permet cette action." },
        { MoniPayErrorTypes.VerificationCodeInvalid.Code, Locale.EnglishTag, "The verification code is incorrect." },
        { MoniPayErrorTypes.VerificationCodeInvalid.Code, Locale.FrenchTag, "Le code de vérification est incorrect." },
        { MoniPayErrorTypes.VerificationCodeExpired.Code, Locale.EnglishTag, "The verification code has expired." },
        { MoniPayErrorTypes.VerificationCodeExpired.Code, Locale.FrenchTag, "Le code de vérification a expiré." },
        { MoniPayErrorTypes.SignUpExpired.Code, Locale.EnglishTag, "The sign-up has expired." },
        { MoniPayErrorTypes.SignUpExpired.Code, Locale.FrenchTag, "L'inscription a expiré." },
        { MoniPayErrorTypes.SignUpAttemptLimit.Code, Locale.EnglishTag, "The verification attempt limit was reached." },
        { MoniPayErrorTypes.SignUpAttemptLimit.Code, Locale.FrenchTag, "La limite de tentatives de vérification est atteinte." },
        { MoniPayErrorTypes.SignUpResendLimit.Code, Locale.EnglishTag, "The verification-code resend limit was reached." },
        { MoniPayErrorTypes.SignUpResendLimit.Code, Locale.FrenchTag, "La limite de renvois du code de vérification est atteinte." },
        { MoniPayErrorTypes.SignUpResendTooSoon.Code, Locale.EnglishTag, "The verification code was requested again too soon." },
        { MoniPayErrorTypes.SignUpResendTooSoon.Code, Locale.FrenchTag, "Le code de vérification a été redemandé trop tôt." },
        { MoniPayErrorTypes.RefreshTokenReused.Code, Locale.EnglishTag, "The refresh token has already been used." },
        { MoniPayErrorTypes.RefreshTokenReused.Code, Locale.FrenchTag, "Le jeton de rafraîchissement a déjà été utilisé." },
        { MoniPayErrorTypes.VerificationDeliveryUnavailable.Code, Locale.EnglishTag, "The verification code could not be sent." },
        { MoniPayErrorTypes.VerificationDeliveryUnavailable.Code, Locale.FrenchTag, "Le code de vérification n'a pas pu être envoyé." },
        { MoniPayErrorTypes.PhoneAlreadyRegistered.Code, Locale.EnglishTag, "This phone number is already registered." },
        { MoniPayErrorTypes.PhoneAlreadyRegistered.Code, Locale.FrenchTag, "Ce numéro de téléphone est déjà enregistré." },
        { MoniPayErrorTypes.EmailAlreadyRegistered.Code, Locale.EnglishTag, "This email address is already registered." },
        { MoniPayErrorTypes.EmailAlreadyRegistered.Code, Locale.FrenchTag, "Cette adresse e-mail est déjà enregistrée." },
        { MoniPayErrorTypes.SignUpTokenInvalid.Code, Locale.EnglishTag, "The sign-up token is invalid or expired." },
        { MoniPayErrorTypes.SignUpTokenInvalid.Code, Locale.FrenchTag, "Le jeton d'inscription est invalide ou expiré." },
        { MoniPayErrorTypes.RegistrationTokenInvalid.Code, Locale.EnglishTag, "The registration token is invalid or expired." },
        { MoniPayErrorTypes.RegistrationTokenInvalid.Code, Locale.FrenchTag, "Le jeton d'enregistrement est invalide ou expiré." },
        { MoniPayErrorTypes.SessionInvalid.Code, Locale.EnglishTag, "The session token is invalid or expired." },
        { MoniPayErrorTypes.SessionInvalid.Code, Locale.FrenchTag, "Le jeton de session est invalide ou expiré." },
    };

    public static TheoryData<string, string, string> HostTitles => new()
    {
        { MoniPayErrorTypes.MalformedJson.Code, Locale.EnglishTag, "The request body is not valid JSON." },
        { MoniPayErrorTypes.MalformedJson.Code, Locale.FrenchTag, "Le corps de la requête n'est pas un JSON valide." },
        { MoniPayErrorTypes.JsonApiDocumentInvalid.Code, Locale.EnglishTag, "The request document is not a valid JSON:API document." },
        { MoniPayErrorTypes.JsonApiDocumentInvalid.Code, Locale.FrenchTag, "Le document de la requête n'est pas un document JSON:API valide." },
        { MoniPayErrorTypes.NotAcceptable.Code, Locale.EnglishTag, "No acceptable response media type was requested." },
        { MoniPayErrorTypes.NotAcceptable.Code, Locale.FrenchTag, "Aucun type de réponse acceptable n'a été demandé." },
        { MoniPayErrorTypes.UnsupportedMediaType.Code, Locale.EnglishTag, "The request media type is not supported." },
        { MoniPayErrorTypes.UnsupportedMediaType.Code, Locale.FrenchTag, "Le type de média de la requête n'est pas pris en charge." },
        { MoniPayErrorTypes.Validation.Code, Locale.EnglishTag, "The request is not valid." },
        { MoniPayErrorTypes.Validation.Code, Locale.FrenchTag, "La requête n'est pas valide." },
        { MoniPayErrorTypes.Internal.Code, Locale.EnglishTag, "An unexpected error occurred." },
        { MoniPayErrorTypes.Internal.Code, Locale.FrenchTag, "Une erreur inattendue s'est produite." },
    };

    [Theory]
    [MemberData(nameof(ValidationCodes_))]
    public void A_validation_code_resolves_in_its_owning_module(string code, string culture, string expected)
    {
        Assert.Equal(expected, Resolve(culture, text => text.FailureDetail(code)));
    }

    [Theory]
    [MemberData(nameof(RefusalTitles))]
    [MemberData(nameof(HostTitles))]
    public void A_problem_code_resolves_to_its_expected_title(string code, string culture, string expected)
    {
        Assert.Equal(expected, Resolve(culture, text => text.Title(code)));
    }

    [Fact]
    public void An_unknown_code_keeps_the_safe_fallback()
    {
        Assert.Equal("mystery", Resolve(Locale.FrenchTag, text => text.Title("mystery")));
    }

    private string Resolve(string culture, Func<MoniPayProblemText, string> read)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            return read(api.Services.GetRequiredService<MoniPayProblemText>());
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
