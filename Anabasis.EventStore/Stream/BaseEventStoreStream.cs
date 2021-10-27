using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
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
        private readonly IConnectionStatusMonitor _connectionMonitor;

        private IDisposable _eventStoreConnectionStatus;
        private IDisposable _eventStreamConnectionDisposable;

        protected Subject<IEvent> _onEventSubject;
        protected BehaviorSubject<bool> ConnectionStatusSubject { get; }
        public bool IsConnected => _connectionMonitor.IsConnected && IsWiredUp;
        public IObservable<bool> OnConnected => _connectionMonitor.OnConnected;
        public string Id { get; }
        protected ILogger Logger { get; }
        public bool IsWiredUp { get; private set; }

        public BaseEventStoreStream(IConnectionStatusMonitor connectionMonitor,
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

            ConnectionStatusSubject = new BehaviorSubject<bool>(false);

        }

        public void Connect()
        {
            if (IsWiredUp) return;

            Logger?.LogDebug($"{Id} => Connecting");

            IsWiredUp = true;

            _eventStoreConnectionStatus = _connectionMonitor.GetEvenStoreConnectionStatus().Subscribe(connectionChanged =>
            {
                Logger?.LogDebug($"{Id} => IsConnected: {connectionChanged.IsConnected}");

                ConnectionStatusSubject.OnNext(connectionChanged.IsConnected);

                if (connectionChanged.IsConnected)
                {

                    OnInitialize(connectionChanged.IsConnected);

                    _eventStreamConnectionDisposable = ConnectToEventStream(connectionChanged.Value)
                      .Subscribe(resolvedEvent =>
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

                        });
                }
                else
                {
                    if (null != _eventStreamConnectionDisposable) _eventStreamConnectionDisposable.Dispose();

                }
            });
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

        protected abstract IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection);
        public virtual void Dispose()
        {
            _eventStoreConnectionStatus.Dispose();
            ConnectionStatusSubject.Dispose();
        }

        protected virtual void OnInitialize(bool isConnected) { }
    }
}
