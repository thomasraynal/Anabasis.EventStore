using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Text;
using EventId = Microsoft.Extensions.Logging.EventId;
using MsLogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Anabasis.EventSore.Shared
{
    public class LoggerForEventStore : ILogger
    {
        private readonly MsLogger _logger;
        private readonly EventId _eventId = new EventId(-1);

        public LoggerForEventStore(MsLogger logger)
        {
            _logger = logger;
        }

        public void Debug(string format, params object[] args)
        {
            _logger.Log(LogLevel.Debug, _eventId, string.Format(format, args), null, null);
        }

        public void Debug(Exception ex, string format, params object[] args)
        {
            _logger.Log(LogLevel.Debug, _eventId, string.Format(format, args), ex, null);
        }

        public void Error(string format, params object[] args)
        {
            _logger.Log(LogLevel.Error, _eventId, string.Format(format, args), null, null);
        }

        public void Error(Exception ex, string format, params object[] args)
        {
            _logger.Log(LogLevel.Error, _eventId, string.Format(format, args), ex, null);
        }

        public void Info(string format, params object[] args)
        {
            _logger.Log(LogLevel.Information, _eventId, string.Format(format, args), null, null);
        }

        public void Info(Exception ex, string format, params object[] args)
        {
            _logger.Log(LogLevel.Information, _eventId, string.Format(format, args), ex, null);
        }
    }
}
