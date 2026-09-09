using Microsoft.Extensions.Time.Testing;

namespace MoniPay.Tests.Support;

public sealed class TestTimeProvider : TimeProvider
{
    private FakeTimeProvider provider = new(DateTimeOffset.UtcNow);

    public override DateTimeOffset GetUtcNow() => provider.GetUtcNow();

    public override TimeZoneInfo LocalTimeZone => provider.LocalTimeZone;

    public override long TimestampFrequency => provider.TimestampFrequency;

    public override long GetTimestamp() => provider.GetTimestamp();

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period) => provider.CreateTimer(callback, state, dueTime, period);

    public void Advance(TimeSpan delta) => provider.Advance(delta);

    public void Set(DateTimeOffset value) => provider = new FakeTimeProvider(value);

    public void Reset() => provider = new FakeTimeProvider(DateTimeOffset.UtcNow);
}
