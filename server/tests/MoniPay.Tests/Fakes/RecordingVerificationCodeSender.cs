using System.Collections.Concurrent;
using MoniPay.Kernel;
using MoniPay.Sessions.Providers;

namespace MoniPay.Tests.Fakes;

/// <summary>
/// A branch-free sender: records every message, adds nothing to the context, and reports one
/// configurable delivery state. It stands in for the host adapter until that adapter lands.
/// </summary>
public sealed class RecordingVerificationCodeSender : IVerificationCodeSender
{
    private readonly ConcurrentQueue<VerificationCodeMessage> messages = new();

    public IReadOnlyList<VerificationCodeMessage> Messages => messages.ToArray();

    /// <summary>What <see cref="GetLatestDeliveryAsync"/> answers for every sign-up.</summary>
    public CodeDeliveryState? Delivery { get; set; } = CodeDeliveryState.Queued;

    public Task EnqueueAsync(VerificationCodeMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public Task<CodeDeliveryState?> GetLatestDeliveryAsync(SignUpId signUpId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Delivery);
    }

    /// <summary>The code in the last message for a recipient: the only place a test reads a code from.</summary>
    public string CodeFor(PhoneNumber recipient) => messages.Last(message => message.Recipient == recipient).Code;

    public IReadOnlyList<VerificationCodeMessage> MessagesFor(SignUpId signUpId) =>
        messages.Where(message => message.SignUpId == signUpId).ToArray();
}
