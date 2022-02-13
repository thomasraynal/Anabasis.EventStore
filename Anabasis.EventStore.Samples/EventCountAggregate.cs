using Anabasis.Common;
using System;
using System.Linq;

namespace Anabasis.EventStore.Samples
{


    public class EventCountAggregate : BaseAggregate
    {

        private static object _syncLock = new();

        public EventCountAggregate()
        {
        }

        public int HitCounter { get; set; }

        public void PrintConsole()
        {
            lock (_syncLock)
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
