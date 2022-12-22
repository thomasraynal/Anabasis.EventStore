using Anabasis.Common;
using Anabasis.ProtoActor.MessageBufferActor;
using Anabasis.ProtoActor.MessageHandlerActor;
using DynamicData;
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
        private readonly IAggregateRepository<TAggregate> _aggregateRepository;
        private readonly CompositeDisposable _cleanUp;
        private readonly IEventTypeProvider _eventTypeProvider;

        public SourceCache<TAggregate, string> SourceCache { get; }
        public bool IsCaughtUp { get; private set; }
        public ILogger<AggregateMessageHandlerProtoActorBase<TAggregate, TAggregateMessageHandlerActorConfiguration>>? Logger { get; }
        public string Id { get; }
        public Exception? LastError { get; protected set; }

        protected TAggregateMessageHandlerActorConfiguration AggregateMessageHandlerActorConfiguration { get; private set; }

        protected AggregateMessageHandlerProtoActorBase(TAggregateMessageHandlerActorConfiguration aggregateMessageHandlerActorConfiguration,
            IAggregateRepository<TAggregate> aggregateRepository,
            IEventTypeProvider eventTypeProvider,
            ILoggerFactory? loggerFactory = null)
        {
            Logger = loggerFactory?.CreateLogger<AggregateMessageHandlerProtoActorBase<TAggregate, TAggregateMessageHandlerActorConfiguration>>();
            Id = this.GetUniqueIdFromType();
            SourceCache = new SourceCache<TAggregate, string>(item => item.EntityId);

            _eventTypeProvider = eventTypeProvider;
            _cleanUp = new CompositeDisposable();
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
            _aggregateRepository = aggregateRepository;

            _cleanUp.Add(SourceCache);

            AggregateMessageHandlerActorConfiguration = aggregateMessageHandlerActorConfiguration;
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

                    await UpdateState();

                    await OnStartCaughtUp(context);

                    context.Send(context.Self, EndCaughtUp.Instance);

                    break;
                case EndCaughtUp:

                    await OnEndCaughtUp(context);

                    IsCaughtUp = true;

                    break;
                case GracefullyStopBufferActorMessage:

                    await OnReceivedGracefullyStop(context);

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

                    await OnEventConsumed(tMessage.Content);

                    await tMessage.Acknowledge();

                    break;

            }
        }


        private async Task UpdateState()
        {
            SourceCache.Clear();

            var getAggregateTasks = AggregateMessageHandlerActorConfiguration.StreamIds.Select(streamId =>
            {
                //handle snapshots
                return _aggregateRepository.GetAggregateByStreamIdFromVersion(
                    streamId: streamId,
                    eventTypeProvider: _eventTypeProvider, 
                    keepEventsOnAggregate: AggregateMessageHandlerActorConfiguration.KeepAppliedEventsOnAggregate);

            }).ToArray();

            await getAggregateTasks.ExecuteAndWaitForCompletion(10);

            foreach (var getAggregateTask in getAggregateTasks)
            {
                SourceCache.AddOrUpdate(getAggregateTask.Result);
            }

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


        private async Task OnEventConsumed(IEvent @event)
        {

            if (@event is IAggregateEvent<TAggregate> aggregateEvent && _eventTypeProvider.CanHandle(@event))
            {

                if (!IsCaughtUp && @event.IsCommand) return;

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

            }

            await ConsumeEvent(@event);

        }

        private async Task ConsumeEvent(IEvent @event)
        {
            try
            {

                Logger?.LogDebug($"{Id} => Receiving event {@event.EntityId} - {@event.GetType()}");

                var candidateHandler = _messageHandlerInvokerCache.GetMethodInfo(GetType(), @event.GetType());

                if (null != candidateHandler)
                {
                    await (Task)candidateHandler.Invoke(this, new object[] { @event });
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
