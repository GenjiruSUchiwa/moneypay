using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace MoniPay.Tests.Support;

/// <summary>
/// Captures every log event the host emits, so a test can assert what was — and was not —
/// logged. Serilog picks the sink up from the host's services through <c>ReadFrom.Services</c>.
/// </summary>
public sealed class CapturedLogs : ILogEventSink
{
    private readonly ConcurrentQueue<string> messages = new();

    public IReadOnlyCollection<string> Messages => messages;

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        string properties = string.Join(
            " ",
            logEvent.Properties.Select(property => $"{property.Key}={property.Value}"));
        messages.Enqueue($"{logEvent.RenderMessage()} {properties}");
    }
}
