using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Security;

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
