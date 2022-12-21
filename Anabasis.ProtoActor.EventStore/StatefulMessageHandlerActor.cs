//using Anabasis.Common;
//using Anabasis.Common.Configuration;
//using Anabasis.EventStore;
//using Anabasis.EventStore.Shared;
//using Anabasis.EventStore.Snapshot;
//using Anabasis.ProtoActor.MessageHandlerActor;
//using DynamicData;
//using EventStore.ClientAPI;
//using Microsoft.Extensions.Logging;
//using Proto;
//using System.Reactive.Disposables;
//using System.Reactive.Linq;
//using System.Reactive.Subjects;
//using System.Runtime.ExceptionServices;

//namespace Anabasis.ProtoActor.EventStore
//{
//    public abstract class StatefulMessageHandlerActor<TAggregateMessageHandlerActorConfiguration, TAggregate> : MessageHandlerProtoActorBase
//        where TAggregate : class, IAggregate, new()
//        where TAggregateMessageHandlerActorConfiguration : IAggregateMessageHandlerActorConfiguration
//    {
//        private readonly ManualResetEventSlim _manualResetEventSlim = new();
//        protected ManualResetEventSlim _caughtingUpEvent = new(true);

//        private CatchupCacheSubscriptionHolder<TAggregate>[]? _catchupCacheSubscriptionHolders;

//        private IConnectionStatusMonitor<IEventStoreConnection> _connectionMonitor;
//        private TAggregateMessageHandlerActorConfiguration _catchupCacheConfiguration;
//        private ISnapshotStrategy? _snapshotStrategy;
//        private ISnapshotStore<TAggregate>? _snapshotStore;
//        private DateTime _lastProcessedEventUtcTimestamp;
//        private SourceCache<TAggregate, string> _cache;
//        private SourceCache<TAggregate, string> _caughtingUpCache;
//        private BehaviorSubject<bool> _isCaughtUpSubject;
//        private BehaviorSubject<bool> _isStaleSubject;

//        public IEventTypeProvider EventTypeProvider { get; private set; }
//        protected TAggregateMessageHandlerActorConfiguration AggregateCacheConfiguration => _catchupCacheConfiguration;
//        public bool UseSnapshot => _catchupCacheConfiguration.UseSnapshot;
//        public IObservable<bool> OnStale => _isStaleSubject.AsObservable();
//        public IObservable<bool> OnCaughtUp => _isCaughtUpSubject.AsObservable();
//        public bool IsStale => _isStaleSubject.Value;
//        public bool IsCaughtUp => _isCaughtUpSubject.Value;
//        public bool IsConnected => _connectionMonitor.IsConnected;

//        public StatefulMessageHandlerActor(TAggregateMessageHandlerActorConfiguration messageHandlerActorConfiguration,
//            IActorConfigurationFactory actorConfigurationFactory,
//            IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
//            ILoggerFactory? loggerFactory = null,
//            ISnapshotStore<TAggregate>? snapshotStore = null,
//            ISnapshotStrategy? snapshotStrategy = null) : base(messageHandlerActorConfiguration, loggerFactory)
//        {
//            var actorType = GetType();
//            var aggregateCacheConfiguration = actorConfigurationFactory.GetAggregateCacheConfiguration<TAggregateMessageHandlerActorConfiguration>(actorType);
//            var eventTypeProvider = actorConfigurationFactory.GetEventTypeProvider(actorType);

//            Setup(connectionMonitor,
//                aggregateCacheConfiguration,
//                eventTypeProvider,
//                snapshotStore,
//                snapshotStrategy);
//        }


//        public StatefulMessageHandlerActor(
//            IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
//            TAggregateMessageHandlerActorConfiguration catchupCacheConfiguration,
//            IEventTypeProvider eventTypeProvider,
//            ILoggerFactory? loggerFactory = null,
//            ISnapshotStore<TAggregate>? snapshotStore = null,
//            ISnapshotStrategy? snapshotStrategy = null) : base(messageHandlerActorConfiguration, loggerFactory)
//        {
//            Setup(connectionMonitor,
//                catchupCacheConfiguration,
//                eventTypeProvider,
//                snapshotStore,
//                snapshotStrategy);
//        }

//        public ICatchupCacheSubscriptionHolder[] GetSubscriptionStates()
//        {
//            if (null == _catchupCacheSubscriptionHolders) return Array.Empty<ICatchupCacheSubscriptionHolder>();

//            return _catchupCacheSubscriptionHolders.ToArray();
//        }


//        private void Setup(
//           IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
//           TAggregateMessageHandlerActorConfiguration catchupCacheConfiguration,
//           IEventTypeProvider eventTypeProvider,
//           ISnapshotStore<TAggregate>? snapshotStore = null,
//           ISnapshotStrategy? snapshotStrategy = null)
//        {
//            EventTypeProvider = eventTypeProvider;

//#nullable disable
//            _cache = new SourceCache<TAggregate, string>(item => item.EntityId);
//            _caughtingUpCache = new SourceCache<TAggregate, string>(item => item.EntityId);
//#nullable enable

//            _catchupCacheConfiguration = catchupCacheConfiguration;
//            _connectionMonitor = connectionMonitor;
//            _snapshotStrategy = snapshotStrategy;
//            _snapshotStore = snapshotStore;
//            _lastProcessedEventUtcTimestamp = DateTime.MinValue;
//            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
//            _isStaleSubject = new BehaviorSubject<bool>(true);

//            if (UseSnapshot && (_snapshotStore == null && _snapshotStrategy != null || _snapshotStore != null && _snapshotStrategy == null))
//            {
//                throw new InvalidOperationException($"Snapshots are activated on {GetType().Name}. To use snapshots both a snapshotStore and a snapshotStrategy are required " +
//                    $"[snapshotStore is null = {snapshotStore == null}, snapshotStrategy is null = {snapshotStrategy == null}]");

//            }
//        }

//        protected void Initialize()
//        {
//            var catchupCacheSubscriptionHolders = new[] { new CatchupCacheSubscriptionHolder<TAggregate>(_catchupCacheConfiguration.CrashAppIfSubscriptionFail) };

//            Initialize(catchupCacheSubscriptionHolders);
//        }

//        protected override Task OnStarted(IContext context)
//        {
//            return ConnectToEventStream();
//        }

//        protected void Initialize(CatchupCacheSubscriptionHolder<TAggregate>[] catchupCacheSubscriptionHolders)
//        {

//            _catchupCacheSubscriptionHolders = catchupCacheSubscriptionHolders;

//            foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
//            {
//                var subscription = catchupCacheSubscriptionHolder.OnCaughtUp.Subscribe(hasStreamSubscriptionCaughtUp =>
//                {

//                    if (hasStreamSubscriptionCaughtUp && !IsCaughtUp)
//                    {
//                        if (_catchupCacheSubscriptionHolders.All(holder => holder.IsCaughtUp))
//                        {

//                            Logger?.LogInformation($"{Id} => OnCaughtUp - IsCaughtUp: {IsCaughtUp}");

//                            if (!IsCaughtUp)
//                            {

//                                _cache.Edit(innerCache =>
//                                {
//                                    Logger?.LogInformation($"{Id} => OnCaughtUp - switch from CaughtingUpCache");

//                                    innerCache.Load(_caughtingUpCache.Items);

//                                    _caughtingUpCache.Clear();

//                                });

//                                _isCaughtUpSubject.OnNext(true);

//                            }

//                            Logger?.LogInformation($"{Id} => OnCaughtUp - IsCaughtUp: {IsCaughtUp}");
//                        }
//                    }
//                    else if (!hasStreamSubscriptionCaughtUp && IsCaughtUp)
//                    {
//                        _isCaughtUpSubject.OnNext(false);
//                    }

//                });

//                AddToCleanup(subscription);

//            }

//            var isStaleSubscription = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ =>
//            {
//                foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
//                {
//                    if (DateTime.UtcNow >
//                        catchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp.Add(_catchupCacheConfiguration.IsStaleTimeSpan))
//                    {
//                        _isStaleSubject.OnNext(true);
//                        break;
//                    }
//                }

//            });

//            AddToCleanup(isStaleSubscription);

//            var caughtUpSignalDisposable = OnCaughtUp.Subscribe(isCaughtUp =>
//            {
//                if (isCaughtUp == false)
//                {
//                    _caughtingUpEvent.Reset();
//                }
//                else if (isCaughtUp == true)
//                {
//                    _caughtingUpEvent.Set();
//                }

//            });

//            AddToCleanup(caughtUpSignalDisposable);

//            _manualResetEventSlim.Set();
//        }

//        protected async override Task ConsumeEvent(IEvent @event)
//        {
//            try
//            {
//                Logger?.LogDebug($"{Id} => Receiving event {@event.EntityId} - {@event.GetType()}");

//                if (@event is IAggregateEvent<TAggregate> aggregateEvent && EventTypeProvider.CanHandle(@event))
//                {

//                    if (!IsCaughtUp && @event.IsCommand) return;

//#nullable disable
//                    var entry = CurrentCache.Lookup(@event.EntityId);
//#nullable enable

//                    TAggregate entity;

//                    if (entry.HasValue)
//                    {
//                        entity = entry.Value;

//                        if (entity.Version == aggregateEvent.EventNumber)
//                        {
//                            return;
//                        }
//                    }
//                    else
//                    {
//                        Logger?.LogDebug($"{Id} => Creating aggregate: {aggregateEvent.EventId} {aggregateEvent.EntityId} - v.{aggregateEvent.EventNumber}");

//                        entity = new TAggregate();
//                        entity.SetEntityId(@event.EntityId);
//                    }

//                    Logger?.LogDebug($"{Id} => Updating aggregate: {aggregateEvent.EventId} {aggregateEvent.EntityId} - v.{aggregateEvent.EventNumber}");

//                    entity.ApplyEvent(aggregateEvent, false, _catchupCacheConfiguration.KeepAppliedEventsOnAggregate);

//                    CurrentCache.AddOrUpdate(entity);

//                }
//                else
//                {
//                   await base.ConsumeEvent(@event);  
//                }

//            }
//            catch (Exception exception)
//            {
//                LastError = exception;

//                await OnError(@event, exception);
//            }
//        }

//        protected abstract Task OnLoadSnapshot(
//            CatchupCacheSubscriptionHolder<TAggregate>[]? catchupCacheSubscriptionHolders,
//            ISnapshotStrategy? snapshotStrategy,
//            ISnapshotStore<TAggregate>? snapshotStore);

//        public IObservableCache<TAggregate, string> AsObservableCache()
//        {
//            return _cache.AsObservableCache();
//        }

//        protected string?[] GetEventsFilters()
//        {
//            var eventTypeFilters = EventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

//            return eventTypeFilters;
//        }

//        protected SourceCache<TAggregate, string> CurrentCache => IsCaughtUp ? _cache : _caughtingUpCache;

//        public Task DisconnectFromEventStream()
//        {
//            if (null == _catchupCacheSubscriptionHolders)
//                throw new ArgumentNullException("_catchupCacheSubscriptionHolders");

//            foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
//            {
//                catchupCacheSubscriptionHolder?.EventStreamConnectionDisposable?.Dispose();
//            }

//            return Task.CompletedTask;
//        }

//        public async Task ConnectToEventStream()
//        {

//            if (null == _catchupCacheSubscriptionHolders)
//            {
//                throw new ArgumentNullException("_catchupCacheSubscriptionHolders");
//            }

//            foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
//            {
//                if (null != catchupCacheSubscriptionHolder.EventStreamConnectionDisposable)
//                {
//                    catchupCacheSubscriptionHolder.EventStreamConnectionDisposable.Dispose();
//                }
//            }

//            await OnLoadSnapshot(_catchupCacheSubscriptionHolders, _snapshotStrategy, _snapshotStore);

//            foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
//            {
//                catchupCacheSubscriptionHolder.EventStreamConnectionDisposable = ConnectToEventStreamInternal(_connectionMonitor.Connection, catchupCacheSubscriptionHolder);

//                AddToCleanup(catchupCacheSubscriptionHolder.EventStreamConnectionDisposable);

//            }
//        }

//        public TAggregate? GetCurrent(string key)
//        {
//            return _cache.Items.FirstOrDefault(item => item.EntityId == key);
//        }

//        public TAggregate[] GetCurrents()
//        {
//            return _cache.Items.ToArray();
//        }

//        private IDisposable ConnectToEventStreamInternal(IEventStoreConnection connection, CatchupCacheSubscriptionHolder<TAggregate> catchupCacheSubscriptionHolder)
//        {

//            void stopSubscription()
//            {
//                if (null != catchupCacheSubscriptionHolder.EventStoreCatchUpSubscription)
//                {
//                    _isCaughtUpSubject.OnNext(false);
//                    catchupCacheSubscriptionHolder.EventStoreCatchUpSubscription.Stop();
//                }
//            }

//            void createNewCatchupSubscription()
//            {
//                stopSubscription();

//                catchupCacheSubscriptionHolder.EventStoreCatchUpSubscription = GetEventStoreCatchUpSubscription(catchupCacheSubscriptionHolder, connection, onEvent, onSubscriptionDropped);
//            }

//            async Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent resolvedEvent)
//            {

//                _manualResetEventSlim.Wait();

//                Logger?.LogDebug($"{Id} => OnEvent {resolvedEvent.Event.EventType} - v.{resolvedEvent.Event.EventNumber}");

//                var recordedEvent = resolvedEvent.Event;

//                var @event = DeserializeEvent(recordedEvent);

//                if (null == @event)
//                {
//                    throw new EventNotSupportedException(recordedEvent);
//                }

//                await ConsumeEvent(@event);

//                if (IsStale) _isStaleSubject.OnNext(false);

//                catchupCacheSubscriptionHolder.LastProcessedEventSequenceNumber = resolvedEvent.Event.EventNumber;
//                catchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp = DateTime.UtcNow;

//                if (IsCaughtUp && UseSnapshot)
//                {
//                    foreach (var aggregate in _cache.Items)
//                    {
//#nullable disable
//                        if (_snapshotStrategy.IsSnapshotRequired(aggregate))
//                        {
//                            Logger?.LogInformation($"{Id} => Snapshoting aggregate => {aggregate.EntityId} {aggregate.GetType()} - v.{aggregate.Version}");

//                            var eventFilter = GetEventsFilters();

//                            aggregate.VersionFromSnapshot = aggregate.Version;

//                            _snapshotStore.Save(eventFilter, aggregate).Wait();

//                        }
//#nullable enable
//                    }
//                }

//            }

//            void onSubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription, SubscriptionDropReason subscriptionDropReason, Exception exception)
//            {

//                switch (subscriptionDropReason)
//                {

//                    case SubscriptionDropReason.UserInitiated:
//                        break;

//                    case SubscriptionDropReason.ConnectionClosed:
//                    case SubscriptionDropReason.EventHandlerException:
//                    case SubscriptionDropReason.CatchUpError:
//                    case SubscriptionDropReason.NotAuthenticated:
//                    case SubscriptionDropReason.AccessDenied:
//                    case SubscriptionDropReason.SubscribingError:
//                    case SubscriptionDropReason.ServerError:
//                    case SubscriptionDropReason.ProcessingQueueOverflow:
//                    case SubscriptionDropReason.MaxSubscribersReached:
//                    case SubscriptionDropReason.PersistentSubscriptionDeleted:
//                    case SubscriptionDropReason.Unknown:
//                    case SubscriptionDropReason.NotFound:
//                    default:

//                        Logger?.LogError(exception, $"{nameof(SubscriptionDropReason)}: [{subscriptionDropReason}] throwed the consumer in an invalid state");

//                        if (catchupCacheSubscriptionHolder.CrashAppIfSubscriptionFail)
//                        {
//                            ExceptionDispatchInfo.Capture(exception).Throw();
//                        }
//                        else
//                        {
//                            Task.Delay(200).Wait();

//                            createNewCatchupSubscription();
//                        }

//                        break;
//                }
//            }

//            createNewCatchupSubscription();

//            return Disposable.Create(() =>
//            {
//                stopSubscription();
//            });

//        }

//        protected abstract EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(
//            CatchupCacheSubscriptionHolder<TAggregate> catchupCacheSubscriptionHolder,
//            IEventStoreConnection connection,
//            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent,
//            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped);

//        private IAggregateEvent<TAggregate> DeserializeEvent(RecordedEvent recordedEvent)
//        {
//            var targetType = EventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

//            if (null == targetType) throw new InvalidOperationException($"{recordedEvent.EventType} cannot be handled");

//#nullable disable

//            var aggregateEvent = _catchupCacheConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IAggregateEvent<TAggregate>;
//            aggregateEvent.EventNumber = recordedEvent.EventNumber;

//#nullable enable

//            return aggregateEvent;

//        }
//    }
//}