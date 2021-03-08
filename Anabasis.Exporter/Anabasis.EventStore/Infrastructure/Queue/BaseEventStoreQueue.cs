using EventStore.ClientAPI;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore.Infrastructure.Queue
{
  public abstract class BaseEventStoreQueue : IEventStoreQueue
  {
    protected readonly IEventStoreQueueConfiguration _eventStoreQueueConfiguration;
    protected readonly IEventTypeProvider _eventTypeProvider;
    private readonly IConnectionStatusMonitor _connectionMonitor;
    private readonly ILogger _logger;

    private IDisposable _eventStoreConnectionStatus;
    private IDisposable _eventStreamConnectionDisposable;

    protected Subject<IEvent> _onEventSubject;
    protected BehaviorSubject<bool> ConnectionStatusSubject { get; }
    public bool IsConnected => _connectionMonitor.IsConnected;
    public IObservable<bool> OnConnected => _connectionMonitor.OnConnected;

    public BaseEventStoreQueue(IConnectionStatusMonitor connectionMonitor,
      IEventStoreQueueConfiguration cacheConfiguration,
      IEventTypeProvider eventTypeProvider,
      ILogger logger = null)
    {

      _logger = logger ?? new DummyLogger();

      _eventStoreQueueConfiguration = cacheConfiguration;
      _eventTypeProvider = eventTypeProvider;
      _connectionMonitor = connectionMonitor;

      _onEventSubject = new Subject<IEvent>();

      ConnectionStatusSubject = new BehaviorSubject<bool>(false);

    }

    protected void InitializeAndRun()
    {
      _eventStoreConnectionStatus = _connectionMonitor.GetEvenStoreConnectionStatus().Subscribe(connectionChanged =>
      {
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
                  if (_eventStoreQueueConfiguration.IgnoreUnknownEvent) return;

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

      return _eventStoreQueueConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IEvent;
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
