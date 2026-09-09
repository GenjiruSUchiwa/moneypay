using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;

namespace MoniPay.Sessions.Security;

internal enum WorkflowCredentialFailure
{
    HeaderMissing,

    SignUpIdMissing,

    UnknownSignUp,

    Expired,

    Consumed,

    Mismatch,
}

internal abstract class WorkflowAuthenticationHandler(
    string scheme,
    SignUpTokenPurpose purpose,
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    MoniPayDbContext database,
    SignUpTokens tokens,
    TimeProvider timeProvider)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = Scheme.Name;
        Context.Items[MoniPayHttpContextItems.AuthenticationProblemCode] = Scheme.Name switch
        {
            SessionsSchemes.SignUp => MoniPayErrorTypes.SignUpTokenInvalid.Code,
            SessionsSchemes.Registration => MoniPayErrorTypes.RegistrationTokenInvalid.Code,
            _ => MoniPayErrorTypes.Unauthorized.Code,
        };

        return base.HandleChallengeAsync(properties);
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (!MatchesScheme(header, Scheme.Name, out string rawToken))
        {
            return Refuse(WorkflowCredentialFailure.HeaderMissing);
        }

        if (!Guid.TryParse(Request.RouteValues[MoniPayClaimTypes.SignUpId]?.ToString(), out Guid signUpIdValue))
        {
            return Refuse(WorkflowCredentialFailure.SignUpIdMissing);
        }

        SignUpId signUpId = new(signUpIdValue);
        SignUp? signUp = await database.SignUps
            .AsNoTracking()
            .SingleOrDefaultAsync(signUp => signUp.Id == signUpId, Context.RequestAborted)
            .ConfigureAwait(false);
        if (signUp is null)
        {
            return Refuse(WorkflowCredentialFailure.UnknownSignUp);
        }

        if (!IsUsable(signUp, timeProvider.GetUtcNow(), out WorkflowCredentialFailure unusable))
        {
            return Refuse(unusable);
        }

        if (StoredDigest(signUp) is not { } stored)
        {
            return Refuse(WorkflowCredentialFailure.Consumed);
        }

        byte[] candidate = tokens.Digest(purpose, signUpId, rawToken);
        if (candidate.Length != stored.Length || !CryptographicOperations.FixedTimeEquals(candidate, stored))
        {
            return Refuse(WorkflowCredentialFailure.Mismatch);
        }

        ClaimsIdentity identity = new([new Claim(MoniPayClaimTypes.SignUpId, signUpIdValue.ToString())], Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    internal static string PresentedCredential(HttpRequest request, string scheme)
    {
        ArgumentNullException.ThrowIfNull(request);

        return MatchesScheme(request.Headers.Authorization, scheme, out string rawToken)
            ? rawToken
            : string.Empty;
    }

    protected abstract byte[]? StoredDigest(SignUp signUp);

    protected abstract bool IsUsable(SignUp signUp, DateTimeOffset now, out WorkflowCredentialFailure unusable);

    private AuthenticateResult Refuse(WorkflowCredentialFailure reason)
    {
        SessionsLog.WorkflowCredentialRefused(Logger, scheme, reason.ToString());
        return AuthenticateResult.Fail("The workflow credential was refused.");
    }

    private static bool MatchesScheme(string? header, string name, out string rawToken)
    {
        rawToken = string.Empty;
        if (string.IsNullOrEmpty(header)
            || header.Length <= name.Length
            || !header.StartsWith(name, StringComparison.OrdinalIgnoreCase)
            || header[name.Length] != ' ')
        {
            return false;
        }

        rawToken = header[(name.Length + 1)..].Trim();
        return Base64Url.IsValid(rawToken);
    }
}
