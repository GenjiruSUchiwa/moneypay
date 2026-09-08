using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Features.SignUps.Complete;

/// <summary>Whether this call provisioned the user, and the bootstrap session's credentials.</summary>
internal sealed record CreateSignUpCompletionResult(bool Created, SessionTokenResult Session);
