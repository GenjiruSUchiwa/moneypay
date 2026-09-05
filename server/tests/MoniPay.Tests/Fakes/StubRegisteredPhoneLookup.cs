using System.Collections.Concurrent;
using MoniPay.Kernel;
using MoniPay.Sessions.Ports;

namespace MoniPay.Tests.Fakes;

/// <summary>
/// A branch-free lookup: records every phone it was asked about and answers with one configured
/// owner. It stands in for the host adapter until that adapter lands.
/// </summary>
public sealed class StubRegisteredPhoneLookup : IRegisteredPhoneLookup
{
    private readonly ConcurrentQueue<PhoneNumber> phones = new();

    public IReadOnlyList<PhoneNumber> Phones => phones.ToArray();

    /// <summary>The user every lookup reports; <c>null</c> means no phone is registered.</summary>
    public UserId? Owner { get; set; }

    public Task<UserId?> FindUserIdAsync(PhoneNumber phone, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        phones.Enqueue(phone);
        return Task.FromResult(Owner);
    }
}
