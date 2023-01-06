using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.Bus;
using Anabasis.ProtoActor.MessageBufferActor;
using Anabasis.ProtoActor.MessageHandlerActor;
using DynamicData;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Mailbox;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.AggregateActor
{
    public abstract class AggregateMessageHandlerProtoActorBase<TAggregate,TAggregateMessageHandlerActorConfiguration> : IActor, IDisposable
        where TAggregateMessageHandlerActorConfiguration : IAggregateMessageHandlerActorConfiguration
        where TAggregate : class, IAggregate, new()
    {
        private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
        private readonly ISnapshotStore<TAggregate>? _snapshotStore;
        private readonly ISnapshotStrategy? _snapshotStrategy;
        private readonly IEventStoreBus _eventStoreBus;
        private readonly CompositeDisposable _cleanUp;
        private readonly IEventTypeProvider _eventTypeProvider;

        //todo: switch to dictionnary
        private readonly ProtoActorCatchupCacheSubscriptionHolder<TAggregate>[] _catchupCacheSubscriptionHolders;

        private bool _isGracefullyStopRequired; 

        public SourceCache<TAggregate, string> SourceCache { get; }
        public bool IsCaughtUp { get; private set; }
        public ILogger<AggregateMessageHandlerProtoActorBase<TAggregate, TAggregateMessageHandlerActorConfiguration>>? Logger { get; }
        public string Id { get; }
        public Exception? LastError { get; protected set; }

        protected TAggregateMessageHandlerActorConfiguration AggregateMessageHandlerActorConfiguration { get; private set; }

        protected AggregateMessageHandlerProtoActorBase(TAggregateMessageHandlerActorConfiguration aggregateMessageHandlerActorConfiguration,
            IEventStoreBus eventStoreBus,
            IEventTypeProvider eventTypeProvider,
            ISnapshotStore<TAggregate>? snapshotStore = null,
            ISnapshotStrategy? snapshotStrategy = null,
            ILoggerFactory? loggerFactory = null)
        {
            Logger = loggerFactory?.CreateLogger<AggregateMessageHandlerProtoActorBase<TAggregate, TAggregateMessageHandlerActorConfiguration>>();
            Id = this.GetUniqueIdFromType();

#nullable disable
            SourceCache = new SourceCache<TAggregate, string>(item => item.EntityId);
#nullable enable

            _eventTypeProvider = eventTypeProvider;
            _cleanUp = new CompositeDisposable();
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
            _snapshotStore = snapshotStore;
            _snapshotStrategy = snapshotStrategy;
            _eventStoreBus = eventStoreBus;
            _isGracefullyStopRequired = false;

            if (aggregateMessageHandlerActorConfiguration.UseSnapshot && (_snapshotStore == null && _snapshotStrategy != null || _snapshotStore != null && _snapshotStrategy == null))
            {
                throw new InvalidOperationException($"Snapshots are activated on {GetType().Name}. To use snapshots both a snapshotStore and a snapshotStrategy are required " +
                    $"[snapshotStore is null = {snapshotStore == null}, snapshotStrategy is null = {snapshotStrategy == null}]");
            }

            _cleanUp.Add(SourceCache);

            AggregateMessageHandlerActorConfiguration = aggregateMessageHandlerActorConfiguration;

            _catchupCacheSubscriptionHolders = aggregateMessageHandlerActorConfiguration.StreamIdAndPositions
                .Select(streamIdAndPosition => new ProtoActorCatchupCacheSubscriptionHolder<TAggregate>(
                    streamIdAndPosition.StreamId,
                    streamIdAndPosition.StreamPosition,
                    aggregateMessageHandlerActorConfiguration.CrashAppIfSubscriptionFail))
                .ToArray();

        }

        public IProtoActorCatchupCacheSubscriptionHolder[] GetSubscriptionStates()
        {
            if (null == _catchupCacheSubscriptionHolders) return Array.Empty<IProtoActorCatchupCacheSubscriptionHolder>();

            return _catchupCacheSubscriptionHolders.ToArray();
        }

        protected string?[] GetEventsFilters()
        {
            var eventTypeFilters = _eventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

            return eventTypeFilters;
        }

        public async Task ReceiveAsync(IContext context)
        {
            var message = context.Message;

            if (null == message)
            {
                return;
            }

            switch (message)
            {
                case SystemMessage:

                    if (message is Started)
                    {

                        IsCaughtUp = false;

                        await OnStarted(context);

                        context.SetReceiveTimeout(AggregateMessageHandlerActorConfiguration.IdleTimeoutFrequency);

                        context.Send(context.Self, StartCaughtUp.Instance);

                    }

                    if (message is ReceiveTimeout)
                    {
                        await OnReceivedIdleTimout(context);
                    }

                    Logger?.LogInformation($"Received SystemMessage => {message.GetType()}");

                    break;


                case StartCaughtUp:

                    await LoadSnapshot();

                    await OnStartCaughtUp(context);

                    context.Send(context.Self, EndCaughtUp.Instance);

                    break;

                case EndCaughtUp:

                    await OnEndCaughtUp(context);

                    IsCaughtUp = true;

                    var streamIdAndVersions = _catchupCacheSubscriptionHolders
                        .Select(holder => new StreamIdAndPosition(holder.StreamId, holder.LastProcessedEventSequenceNumber))
                        .ToArray();

                    var catchUpSubscriptions =
                        _eventStoreBus.SubscribeToManyStreams(streamIdAndVersions,
                        ((message, timeSpan) =>
                            {
                                context.Send(context.Self, message);

                            }), _eventTypeProvider);

                    _cleanUp.Add(catchUpSubscriptions);

                    break;

                case GracefullyStopActorMessage:

                    _isGracefullyStopRequired = true;

                    await OnReceivedGracefullyStop(context);

                    _cleanUp.Dispose();

                    context.Stop(context.Self);

                    break;

                default:
                    throw new InvalidOperationException($"Message {message.GetType()} is not of type {typeof(IMessage)}");

                case BufferTimeoutDelayMessage:
                case IMessage:

                    if (!IsCaughtUp)
                    {
                        throw new InvalidOperationException("not caught up");
                    }

                    Logger?.LogInformation($"Received message => {message.GetType()}");

                    if (message is not IMessage tMessage)
                    {
                        throw new InvalidOperationException($"Message {message.GetType()} is not of type {typeof(IMessage)}");
                    }

                    if (_isGracefullyStopRequired)
                    {
                        await tMessage.NotAcknowledge();
                        return;
                    }

                    await HandleEventConsumed(tMessage.Content);

                    await tMessage.Acknowledge();

                    Console.WriteLine("got one message");

                    await OnMessageConsumed(context);

                    break;

            }
        }

        private async Task HandleSaveSnapshot()
        {
            if (IsCaughtUp && AggregateMessageHandlerActorConfiguration.UseSnapshot)
            {
                foreach (var aggregate in SourceCache.Items)
                {
#nullable disable
                    if (_snapshotStrategy.IsSnapshotRequired(aggregate))
                    {
                        Logger?.LogInformation($"{Id} => Snapshoting aggregate => {aggregate.EntityId} {aggregate.GetType()} - v.{aggregate.Version}");

                        var eventFilter = GetEventsFilters();

                        aggregate.VersionFromSnapshot = aggregate.Version;

                        await _snapshotStore.Save(eventFilter, aggregate);

                    }
#nullable enable
                }
            }
        }

        private async Task LoadSnapshot()
        {

            if (AggregateMessageHandlerActorConfiguration.UseSnapshot)
            {

#nullable disable

                var eventTypeFilter = GetEventsFilters();

                var getAggregateTasks = _catchupCacheSubscriptionHolders.Select(async catchupCacheSubscriptionHolder =>
                {
                    var snapshot = await _snapshotStore.GetByVersionOrLast(catchupCacheSubscriptionHolder.StreamId, eventTypeFilter);

                    if (null == snapshot)
                    {

                        catchupCacheSubscriptionHolder.CurrentSnapshotEventVersion = snapshot.VersionFromSnapshot;
                        catchupCacheSubscriptionHolder.LastProcessedEventSequenceNumber = snapshot.VersionFromSnapshot;
                        catchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp = DateTime.UtcNow;

                        Logger?.LogInformation($"{Id} => OnLoadSnapshot - EntityId: {snapshot.EntityId} StreamId: {snapshot.EntityId}");

                        SourceCache.AddOrUpdate(snapshot);
                    }

                }).ToArray();

                await getAggregateTasks.ExecuteAndWaitForCompletion(10);

#nullable disable

            }
        }

        protected virtual Task OnMessageConsumed(IContext context)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnEndCaughtUp(IContext context)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnStartCaughtUp(IContext context)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnStarted(IContext context)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnReceivedGracefullyStop(IContext context)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnReceivedIdleTimout(IContext context)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnError(IEvent source, Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();

            return Task.CompletedTask;
        }

        private async Task HandleEventConsumed(IEvent @event)
        {
            try
            {

                if (@event is IAggregateEvent<TAggregate> aggregateEvent && _eventTypeProvider.CanHandle(@event))
                {

                    if (!IsCaughtUp && @event.IsCommand) return;

                    Logger?.LogDebug($"{Id} => Receiving aggregate event {@event.EntityId} - {@event.GetType()}");
#nullable disable
                    var entry = SourceCache.Lookup(@event.EntityId);
#nullable enable

                    TAggregate entity;

                    if (entry.HasValue)
                    {
                        entity = entry.Value;

                        if (entity.Version == aggregateEvent.EventNumber)
                        {
                            return;
                        }
                    }
                    else
                    {
                        Logger?.LogDebug($"{Id} => Creating aggregate: {aggregateEvent.EventId} {aggregateEvent.EntityId} - v.{aggregateEvent.EventNumber}");

                        entity = new TAggregate();
                        entity.SetEntityId(@event.EntityId);
                    }

                    Logger?.LogDebug($"{Id} => Updating aggregate: {aggregateEvent.EventId} {aggregateEvent.EntityId} - v.{aggregateEvent.EventNumber}");

                    entity.ApplyEvent(aggregateEvent, false, AggregateMessageHandlerActorConfiguration.KeepAppliedEventsOnAggregate);

                    SourceCache.AddOrUpdate(entity);

                    var catchupCacheSubscriptionHolder = _catchupCacheSubscriptionHolders.FirstOrDefault(holder => holder.StreamId == aggregateEvent.EntityId);

                    catchupCacheSubscriptionHolder.LastProcessedEventSequenceNumber = aggregateEvent.EventNumber;
                    catchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp = DateTime.UtcNow;

                    await HandleSaveSnapshot();

                }
                else
                {
                    Logger?.LogDebug($"{Id} => Receiving event {@event.EntityId} - {@event.GetType()}");
                }

                var candidateHandler = _messageHandlerInvokerCache.GetMethodInfo(GetType(), @event.GetType());

                if (null != candidateHandler)
                {
#nullable disable
                    await (Task)candidateHandler.Invoke(this, new object[] { @event });
#nullable enable
                }

                if (!AggregateMessageHandlerActorConfiguration.SwallowUnkwownEvents)
                {
                    throw new InvalidOperationException($"{Id} cannot handle event {@event.GetType()}");
                }

            }
            catch (Exception exception)
            {
                LastError = exception;

                await OnError(@event, exception);
            }

        }

        public void AddToCleanup(IDisposable disposable)
        {
            _cleanUp.Add(disposable);
        }
        public override bool Equals(object? obj)
        {
            return obj is MessageHandlerProtoActorBase<TAggregateMessageHandlerActorConfiguration> actor && Id == actor.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }
    }
}
