using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Features.SignUps.Complete;

internal sealed record CreateSignUpCompletionResult(bool Created, SessionTokenResult Session);
