using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace FoodTruckApi.Tests;

internal sealed record LogEntry(string Category, LogLevel Level, string Message);

/// <summary>An <see cref="ILoggerProvider"/> that keeps every log entry for assertions.</summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, Entries);

    public void Dispose()
    {
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentQueue<LogEntry> _entries;

        public RecordingLogger(string category, ConcurrentQueue<LogEntry> entries)
        {
            _category = category;
            _entries = entries;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _entries.Enqueue(new LogEntry(_category, logLevel, formatter(state, exception)));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
