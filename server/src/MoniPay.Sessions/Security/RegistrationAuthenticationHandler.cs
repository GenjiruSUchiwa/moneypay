using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Security;

/// <summary>
/// The <see cref="SessionsSchemes.Registration"/> scheme: the credential issued at phone
/// verification, usable to complete the sign-up. Its lifetime is the remaining sign-up lifetime
/// capped at ten minutes from its issue; a completed sign-up stays completable so a safe retry
/// can return, until that lifetime ends.
/// </summary>
internal sealed class RegistrationAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    MoniPayDbContext database,
    SignUpTokens tokens,
    TimeProvider timeProvider)
    : WorkflowAuthenticationHandler(
        SessionsSchemes.Registration,
        SignUpTokenPurpose.Registration,
        options,
        logger,
        encoder,
        database,
        tokens,
        timeProvider)
{
    protected override byte[]? StoredDigest(SignUp signUp) => signUp.RegistrationTokenDigest;

    protected override bool IsUsable(SignUp signUp, DateTimeOffset now, out WorkflowCredentialFailure unusable)
    {
        ProblemType? refusal = signUp.CompletionRefusal(now);
        unusable = refusal == MoniPayErrorTypes.SignUpStateInvalid
            ? WorkflowCredentialFailure.Consumed
            : WorkflowCredentialFailure.Expired;
        return refusal is null;
    }
}
