using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

namespace Anabasis.EventStore.Actor
{
    public class DummyLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return Disposable.Empty;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }
    }

    public class DummyLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DummyLogger();
        }

        public void Dispose()
        {
        }
    }
}
