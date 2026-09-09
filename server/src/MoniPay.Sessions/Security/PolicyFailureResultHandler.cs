using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace MoniPay.Sessions.Security;

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
