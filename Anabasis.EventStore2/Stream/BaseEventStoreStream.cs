using Anabasis.Common;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore.Stream
{
    public abstract class BaseEventStoreStream : IEventStoreStream
    {
        protected readonly IEventStoreStreamConfiguration _eventStoreStreamConfiguration;
        protected readonly IEventTypeProvider _eventTypeProvider;
        private readonly IConnectionStatusMonitor<IEventStoreConnection> _connectionMonitor;

        private IDisposable? _eventStreamConnectionDisposable;

        protected Subject<IMessage> _onMessageSubject;
        public bool IsConnected => _connectionMonitor.IsConnected;
        public IObservable<bool> OnConnected => _connectionMonitor.OnConnected;
        public string Id { get; }
        protected ILogger? Logger { get; }

        public BaseEventStoreStream(IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
          IEventStoreStreamConfiguration cacheConfiguration,
          IEventTypeProvider eventTypeProvider,
          ILoggerFactory? loggerFactory = null)
        {

            Logger = loggerFactory?.CreateLogger(GetType());

            Id = $"{GetType()}-{Guid.NewGuid()}";

            _eventStoreStreamConfiguration = cacheConfiguration;
            _eventTypeProvider = eventTypeProvider;
            _connectionMonitor = connectionMonitor;

            _onMessageSubject = new Subject<IMessage>();

        }

        protected IEvent? GetEvent(ResolvedEvent resolvedEvent)
        {

            var recordedEvent = resolvedEvent.Event;

            var eventType = _eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

            if (null == eventType)
            {
                if (_eventStoreStreamConfiguration.IgnoreUnknownEvent) return null;

                throw new InvalidOperationException($"Event {recordedEvent.EventType} is not registered");
            }

            var @event = DeserializeEvent(recordedEvent);

            return @event;

        }

        protected abstract IMessage CreateMessage(IEvent @event, ResolvedEvent resolvedEvent);

        protected void OnResolvedEvent(ResolvedEvent resolvedEvent)
        {
            var @event = GetEvent(resolvedEvent);
   
            if (null == @event) return;

            var message = CreateMessage(@event, resolvedEvent);

            _onMessageSubject.OnNext(message);
        }

        public void Connect()
        {

            if (null != _eventStreamConnectionDisposable)
                _eventStreamConnectionDisposable.Dispose();

            _eventStreamConnectionDisposable = ConnectToEventStream(_connectionMonitor.Connection);

        }

        public IObservable<IMessage> OnMessage()
        {
            return _onMessageSubject.AsObservable();
        }

        private IEvent DeserializeEvent(RecordedEvent recordedEvent)
        {
#nullable disable

            var targetType = _eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

            return _eventStoreStreamConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IEvent;

#nullable enable
        }

        protected abstract IDisposable ConnectToEventStream(IEventStoreConnection connection);

        public virtual void Dispose()
        {
            if (!_onMessageSubject.IsDisposed)
            {
                _onMessageSubject.OnCompleted();
                _onMessageSubject.Dispose();
            }

            if (null != _eventStreamConnectionDisposable) _eventStreamConnectionDisposable.Dispose();
        }

        public abstract void Disconnect();
    }
}
