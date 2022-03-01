using Anabasis.Common;
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
    }
}
