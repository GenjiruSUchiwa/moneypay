using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Complete;

internal sealed record CreateSignUpCompletionCommand(
    PersonName FirstName,
    PersonName LastName,
    EmailAddress Email,
    Guid DeviceId);
