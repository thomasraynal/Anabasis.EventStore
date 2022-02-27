using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore.Stream
{
    public abstract class BaseEventStoreStream : IEventStoreStream
    {
        protected readonly IEventStoreStreamConfiguration _eventStoreStreamConfiguration;
        protected readonly IEventTypeProvider _eventTypeProvider;
        private readonly IConnectionStatusMonitor<IEventStoreConnection> _connectionMonitor;

        private IDisposable _eventStreamConnectionDisposable;

        protected Subject<IEvent> _onEventSubject;
        public bool IsConnected => _connectionMonitor.IsConnected && IsWiredUp;
        public IObservable<bool> OnConnected => _connectionMonitor.OnConnected;
        public string Id { get; }
        protected ILogger Logger { get; }
        public bool IsWiredUp { get; protected set; }

        public BaseEventStoreStream(IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
          IEventStoreStreamConfiguration cacheConfiguration,
          IEventTypeProvider eventTypeProvider,
          ILogger logger = null)
        {

            Logger = logger;

            Id = $"{GetType()}-{Guid.NewGuid()}";

            _eventStoreStreamConfiguration = cacheConfiguration;
            _eventTypeProvider = eventTypeProvider;
            _connectionMonitor = connectionMonitor;
            IsWiredUp = false;

            _onEventSubject = new Subject<IEvent>();

        }

        protected void OnResolvedEvent(ResolvedEvent resolvedEvent)
        {

            var recordedEvent = resolvedEvent.Event;

            var eventType = _eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

            if (null == eventType)
            {
                if (_eventStoreStreamConfiguration.IgnoreUnknownEvent) return;

                throw new InvalidOperationException($"Event {recordedEvent.EventType} is not registered");
            }

            var @event = DeserializeEvent(recordedEvent);

            _onEventSubject.OnNext(@event);
        }

        public void Connect()
        {
            if (IsWiredUp) return;

            IsWiredUp = true;

            OnInitialize();

            if (null != _eventStreamConnectionDisposable)
                _eventStreamConnectionDisposable.Dispose();

            _eventStreamConnectionDisposable = ConnectToEventStream(_connectionMonitor.Connection);

        }

        public IObservable<IEvent> OnEvent()
        {
            return _onEventSubject.AsObservable();
        }

        private IEvent DeserializeEvent(RecordedEvent recordedEvent)
        {
            var targetType = _eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

            return _eventStoreStreamConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IEvent;
        }

        protected abstract IDisposable ConnectToEventStream(IEventStoreConnection connection);
        public virtual void Dispose()
        {
            _onEventSubject.OnCompleted();
            _onEventSubject.Dispose();

            if (null != _eventStreamConnectionDisposable) _eventStreamConnectionDisposable.Dispose();
        }

        protected virtual void OnInitialize() { }

        public abstract void Disconnect();
    }
}
