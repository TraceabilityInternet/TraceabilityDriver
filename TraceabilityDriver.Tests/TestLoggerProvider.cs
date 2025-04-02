using Microsoft.Extensions.Logging;

namespace TraceabilityDriver.Tests
{
    /// <summary>
    /// Custom in-memory logger provider to capture logs during integration tests
    /// </summary>
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly Dictionary<string, TestLogger> _loggers = new();

        public List<(LogLevel Level, string Message, Exception? Exception)> LogEntries { get; } = new();

        public ILogger CreateLogger(string categoryName)
        {
            if (!_loggers.TryGetValue(categoryName, out var logger))
            {
                logger = new TestLogger(categoryName, this);
                _loggers[categoryName] = logger;
            }
            return logger;
        }

        public void Dispose()
        {
            _loggers.Clear();
            LogEntries.Clear();
        }

        private class TestLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly TestLoggerProvider _provider;

            public TestLogger(string categoryName, TestLoggerProvider provider)
            {
                _categoryName = categoryName;
                _provider = provider;
            }

            public IDisposable BeginScope<TState>(TState state) => new NoOpDisposable();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                _provider.LogEntries.Add((logLevel, formatter(state, exception), exception));
            }

            private class NoOpDisposable : IDisposable
            {
                public void Dispose() { }
            }
        }
    }
}
