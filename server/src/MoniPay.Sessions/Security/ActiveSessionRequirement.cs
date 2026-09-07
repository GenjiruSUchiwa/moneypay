using Microsoft.AspNetCore.Authorization;

namespace MoniPay.Sessions.Security;

/// <summary>
/// The <see cref="MoniPayPolicies.AuthenticatedUser"/> policy's extra requirement: the bearer
/// ticket is not enough — the session the <c>sid</c> claim names must still be active. The
/// handler answers it with one scoped query per request; there is no cache.
/// </summary>
internal sealed class ActiveSessionRequirement : IAuthorizationRequirement;
