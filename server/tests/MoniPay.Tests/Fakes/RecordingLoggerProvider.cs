using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MoniPay.Tests.Fakes;

/// <summary>Keeps every log event the host writes, so a test can assert on level, message and state.</summary>
public sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> entries = new();

    public sealed record LogEntry(string Category, LogLevel Level, EventId EventId, string Message, IReadOnlyList<KeyValuePair<string, object?>> State, Exception? Exception);

    public IReadOnlyList<LogEntry> Entries => entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, entries);

    public void Dispose()
    {
    }

    private sealed class Logger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            IReadOnlyList<KeyValuePair<string, object?>> values = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            entries.Enqueue(new LogEntry(category, logLevel, eventId, formatter(state, exception), values, exception));
        }
    }
}
