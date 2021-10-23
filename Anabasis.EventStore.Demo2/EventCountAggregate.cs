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

    public class EventCountAggregate : BaseAggregate<string>
    {
        public EventCountAggregate()
        {
        }

        public int HitCounter { get; set; }

        public void PrintConsole()
        {
            var header = $"{nameof(EventCountAggregate)} - {StreamId} - {HitCounter}";

            Console.WriteLine(header);

            var groupedEvents = AppliedEvents.GroupBy(ev => ev.GetType().Name);

            foreach(var events in groupedEvents)
            {
                Console.WriteLine($"    {events.Key} : {events.Count()}");

            }

        }
    }


}
