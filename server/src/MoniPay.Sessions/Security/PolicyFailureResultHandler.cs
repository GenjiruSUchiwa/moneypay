using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace MoniPay.Sessions.Security;

/// <summary>
/// Turns "authenticated, but the policy refused" into a 401 challenge. The default middleware
/// answers that case with 403 — the right status for a valid principal lacking a role, wrong
/// here: a revoked session or a credential bound to another sign-up is a credential problem, and
/// every 401 the module produces names its scheme. The framework's own handling of unauthenticated
/// requests (challenge with <c>WWW-Authenticate</c>) is kept untouched.
/// </summary>
internal sealed class PolicyFailureResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult policyResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(policyResult);

        if (policyResult.Forbidden
            && policyResult.AuthorizationFailure?.FailedRequirements.Any(requirement =>
                requirement is ActiveSessionRequirement or RegistrationRouteRequirement) == true)
        {
            policyResult = PolicyAuthorizationResult.Challenge();
        }

        await defaultHandler.HandleAsync(next, context, policy, policyResult).ConfigureAwait(false);
    }
}
