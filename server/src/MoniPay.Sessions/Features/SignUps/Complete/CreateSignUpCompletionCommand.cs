using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Complete;

/// <summary>The profile the client submits with the registration token, and the device to open the session for.</summary>
internal sealed record CreateSignUpCompletionCommand(
    PersonName FirstName,
    PersonName LastName,
    EmailAddress Email,
    Guid DeviceId);
