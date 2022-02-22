using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.Common;
using Anabasis.Common.Configuration;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseEventStoreStatelessActor : BaseStatelessActor, IEventStoreStatelessActor
    {

        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly List<IEventStoreStream> _eventStoreStreams;

        protected BaseEventStoreStatelessActor(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfiguration,loggerFactory)
        {
            _eventStoreRepository = eventStoreRepository;
            _eventStoreStreams = new List<IEventStoreStream>();
        }

        protected BaseEventStoreStatelessActor(IActorConfigurationFactory actorConfigurationFactory, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
            _eventStoreRepository = eventStoreRepository;
            _eventStoreStreams = new List<IEventStoreStream>();
        }

        public void SubscribeToEventStream(IEventStoreStream eventStoreStream, bool closeSubscriptionOnDispose = false)
        {
            eventStoreStream.Connect();

            Logger?.LogDebug($"{Id} => Subscribing to {eventStoreStream.Id}");

            _eventStoreStreams.Add(eventStoreStream);

            var onEventReceivedDisposable = eventStoreStream.OnEvent().Subscribe(@event => OnEventReceived(@event));

            if (closeSubscriptionOnDispose)
            {
                AddDisposable(eventStoreStream);
            }

            AddDisposable(onEventReceivedDisposable);
        }

        public async Task EmitEventStore<TEvent>(TEvent @event, TimeSpan? timeout = null, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent
        {

            if (!_eventStoreRepository.IsConnected)
            {
               await WaitUntilConnected(timeout);
            }

            Logger?.LogDebug($"{Id} => Emitting {@event.EntityId} - {@event.GetType()}");

            await _eventStoreRepository.Emit(@event, extraHeaders);
        }

        public Task<TCommandResult> SendEventStore<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse
        {
            Logger?.LogDebug($"{Id} => Sending command {command.EntityId} - {command.GetType()}");

            var taskSource = new TaskCompletionSource<ICommandResponse>();

            var cancellationTokenSource = null != timeout ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();

            cancellationTokenSource.Token.Register(() => taskSource.TrySetCanceled(), false);

            PendingCommands[command.EventID] = taskSource;

            _eventStoreRepository.Emit(command).Wait();

            return taskSource.Task.ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully) return (TCommandResult)task.Result;
                if (task.IsCanceled) throw new TimeoutException($"Command {command.EntityId} timeout");

                throw task.Exception;

            }, cancellationTokenSource.Token);

        }

    }
}
