using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Lamar;
using System;

namespace Anabasis.EventStore.Demo2
{
    public class DemoSystemRegistry : ServiceRegistry
    {
        public DemoSystemRegistry()
        {
        }
    }

    class Program_ReadManyStreamFromStartCache
    {
        public static UserCredentials EventStoreUserCredentials = new UserCredentials("admin", "changeit");

        public static string EventStoreUrl
        {

            get
            {
                return "tcp://localhost:1113";
            }
        }

        static void Main(string[] args)
        {
            var connectionSettings = ConnectionSettings
                .Create()
                .DisableTls()
                .DisableServerCertificateValidation()
                .EnableVerboseLogging()
                .UseDebugLogger()
                .SetDefaultUserCredentials(EventStoreUserCredentials)
                .Build();

            var entityOne = "entityOne";
            var entityTwo = "entityTwo";
            var entityThree = "entityThree";
            
            var eventTypeProvider = new DefaultEventTypeProvider<string, EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;

            var eventCountActor = StatefulActorBuilder<EventCountActor, string, EventCountAggregate, DemoSystemRegistry>
                                       .Create(EventStoreUrl, connectionSettings)
                                       .WithReadManyStreamFromStartCache(
                                            new[] { entityOne, entityTwo, entityThree },
                                            eventTypeProvider: eventTypeProvider,
                                            getMultipleStreamsCatchupCacheConfiguration : builder => builder.KeepAppliedEventsOnAggregate = true)
                                       .Build();

            eventCountActor.State.AsObservableCache().Connect().Subscribe((changes) =>
            {
                foreach(var change in changes)
                {
                    change.Current.PrintConsole();
                }
            });

            var rand = new Random();
            var position = 0;
           
            while (true)
            {
                Console.ReadLine();

                var entity = rand.Next(0, 3);

                var target = entityOne;

                if (entity == 1)
                    target = entityOne;
                else if (entity == 2)
                    target = entityTwo;
                else
                    target = entityThree;


                if (rand.Next(0, 2) == 1)
                        eventCountActor.Emit(new EventCountOne(position++, target, Guid.NewGuid())).Wait();
                    else
                        eventCountActor.Emit(new EventCountTwo(position++, target, Guid.NewGuid())).Wait();

            }


        }
    }
}
