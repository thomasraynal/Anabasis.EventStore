using Anabasis.Common;
using Anabasis.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventHubs
{
    public static class EventHubBusExtensions
    {
        public static void SubscribeToEventHub(this IWorker worker)
        {
            var eventHubBus = worker.GetConnectedBus<IEventHubBus>();

            eventHubBus.SubscribeToEventHub(async (messages, cancellationToken) =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                await worker.Handle(messages);

            });
        }
    }
}
