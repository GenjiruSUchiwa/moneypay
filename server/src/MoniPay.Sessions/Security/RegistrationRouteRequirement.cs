using Microsoft.AspNetCore.Authorization;

namespace MoniPay.Sessions.Security;

/// <summary>
/// The <see cref="MoniPayPolicies.Registration"/> policy's extra requirement: the registration
/// credential the request authenticated with must be bound to the sign-up the route names. A
/// valid token for sign-up A is a refusal on sign-up B's route.
/// </summary>
internal sealed class RegistrationRouteRequirement : IAuthorizationRequirement;
