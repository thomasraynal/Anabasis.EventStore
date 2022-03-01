using Anabasis.Common;
using Anabasis.EventStore.Stream;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public static class EventStoreBusExtensions
    {
        public static void SubscribeToEventStream(this IActor actor, IEventStoreStream eventStoreStream, bool closeSubscriptionOnDispose = false)
        {
            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            eventStoreBus.SubscribeToEventStream(eventStoreStream, actor.OnMessageReceived, closeSubscriptionOnDispose);
        }

        public static async Task EmitEventStore<TEvent>(this IActor actor, TEvent @event, TimeSpan? timeout = null, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent
        {
            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            await eventStoreBus.EmitEventStore(@event, timeout, extraHeaders);
        }

        public static async Task<TCommandResult> SendEventStore<TCommandResult>(this IActor actor, ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse
        {
            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            return await eventStoreBus.SendEventStore<TCommandResult>(command, timeout);
        }

        public static void SubscribeFromEndToAllStreams(
            Action<SubscribeFromEndEventStoreStreamConfiguration> getSubscribeFromEndEventStoreStreamConfiguration = null,
            IEventTypeProvider eventTypeProvider = null)
        {

            var getSubscribeFromEndEventStoreStream = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>((connectionMonitor, loggerFactory) =>
            {
                var subscribeFromEndEventStoreStreamConfiguration = new SubscribeFromEndEventStoreStreamConfiguration();

                getSubscribeFromEndEventStoreStreamConfiguration?.Invoke(subscribeFromEndEventStoreStreamConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

                var subscribeFromEndEventStoreStream = new SubscribeFromEndEventStoreStream(
                  connectionMonitor,
                  subscribeFromEndEventStoreStreamConfiguration,
                  eventProvider, loggerFactory);

                return subscribeFromEndEventStoreStream;

            });

            _streamsToRegisterTo.Add(getSubscribeFromEndEventStoreStream);

            return this;
        }
    }
}
