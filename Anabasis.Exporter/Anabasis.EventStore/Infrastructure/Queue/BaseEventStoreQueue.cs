using EventStore.ClientAPI;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore.Infrastructure.Queue
{
  public abstract class BaseEventStoreQueue<TKey> : IEventStoreQueue<TKey>
  {
    protected readonly IEventStoreQueueConfiguration<TKey> _eventStoreQueueConfiguration;
    protected readonly IEventTypeProvider<TKey> _eventTypeProvider;
    private readonly IConnectionStatusMonitor _connectionMonitor;
    private readonly ILogger _logger;

    private IDisposable _eventStoreConnectionStatus;
    private IDisposable _eventStreamConnectionDisposable;

    protected Subject<IEntityEvent<TKey>> _onEventSubject;
    protected BehaviorSubject<bool> ConnectionStatusSubject { get; }
    public bool IsConnected => _connectionMonitor.IsConnected;
    public IObservable<bool> OnConnected => _connectionMonitor.OnConnected;

    public BaseEventStoreQueue(IConnectionStatusMonitor connectionMonitor,
      IEventStoreQueueConfiguration<TKey> cacheConfiguration,
      IEventTypeProvider<TKey> eventTypeProvider,
      ILogger logger = null)
    {

      _logger = logger ?? new DummyLogger();

      _eventStoreQueueConfiguration = cacheConfiguration;
      _eventTypeProvider = eventTypeProvider;
      _connectionMonitor = connectionMonitor;

      _onEventSubject = new Subject<IEntityEvent<TKey>>();

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

                var @eventType = _eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

                var @event = (IEntityEvent<TKey>)Activator.CreateInstance(@eventType);

                _onEventSubject.OnNext(@event);

              });
        }
        else
        {

          if (null != _eventStreamConnectionDisposable) _eventStreamConnectionDisposable.Dispose();

        }
      });
    }

    public IObservable<IEntityEvent<TKey>> OnEvent()
    {
      return _onEventSubject.AsObservable();
    }

    protected abstract IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection);
    public void Dispose()
    {

      OnDispose();

      _eventStoreConnectionStatus.Dispose();
      ConnectionStatusSubject.Dispose();
    }

    protected virtual void OnInitialize(bool isConnected) { }

    protected virtual void OnDispose() { }

  }
}
