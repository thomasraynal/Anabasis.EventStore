using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

namespace Anabasis.EventStore.Infrastructure
{
  public class DummyLogger : ILogger
  {
    public IDisposable BeginScope<TState>(TState state)
    {
      return Disposable.Empty;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
      return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
    }
  }
}
