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
/// The <see cref="SessionsSchemes.SignUp"/> scheme: the credential a start hands out, usable
/// until the phone is verified. Verification publishes the registration credential and destroys
/// the sign-up digest, so the token refuses for good from that moment.
/// </summary>
internal sealed class SignUpAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    MoniPayDbContext database,
    SignUpTokens tokens,
    TimeProvider timeProvider)
    : WorkflowAuthenticationHandler(
        SessionsSchemes.SignUp,
        SignUpTokenPurpose.SignUp,
        options,
        logger,
        encoder,
        database,
        tokens,
        timeProvider)
{
    protected override byte[]? StoredDigest(SignUp signUp) => signUp.SignUpTokenDigest;

    protected override bool IsUsable(SignUp signUp, DateTimeOffset now, out WorkflowCredentialFailure unusable)
    {
        if (signUp.Status == SignUpStatus.Expired || signUp.ExpiresAt <= now)
        {
            unusable = WorkflowCredentialFailure.Expired;
            return false;
        }

        unusable = default;
        return true;
    }
}
