using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;

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
    /// <summary>How long a registration credential outlives its issue, whatever the sign-up has left.</summary>
    internal static readonly TimeSpan MaximumLifetime = TimeSpan.FromMinutes(10);

    protected override byte[]? StoredDigest(SignUp signUp) => signUp.RegistrationTokenDigest;

    protected override bool IsUsable(SignUp signUp, DateTimeOffset now, out WorkflowCredentialFailure unusable)
    {
        if (signUp.Status == SignUpStatus.Expired
            || signUp.ExpiresAt <= now
            || signUp.VerifiedAt is not { } issuedAt
            || issuedAt + MaximumLifetime <= now)
        {
            unusable = WorkflowCredentialFailure.Expired;
            return false;
        }

        if (signUp.Status is not (SignUpStatus.PhoneVerified or SignUpStatus.Completed))
        {
            unusable = WorkflowCredentialFailure.Consumed;
            return false;
        }

        unusable = default;
        return true;
    }
}
