using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Samples
{
   

    public class EventCountAggregate : BaseAggregate
    {

        private static object _locker = new();

        public EventCountAggregate()
        {
        }

        public int HitCounter { get; set; }

        public void PrintConsole()
        {
            lock (_locker)
            {

                var header = $"{nameof(EventCountAggregate)} - {EntityId} - {HitCounter}";

                Console.WriteLine(header);

                var groupedEvents = AppliedEvents.GroupBy(ev => ev.GetType().Name);

                foreach (var events in groupedEvents)
                {
                    Console.WriteLine($"    {events.Key} : {events.Count()}");

                }

            }

        }
    }


}
