using Anabasis.Common;
using Anabasis.EventStore.Cache;
using EventStore.ClientAPI;
using System;
using System.Linq;

namespace Anabasis.EventStore.Samples
{
    public static class Do
    {

        public static ConnectionSettings GetConnectionSettings()
        {
            return ConnectionSettings
                .Create()
                .DisableTls()
                .DisableServerCertificateValidation()
                .EnableVerboseLogging()
                .UseDebugLogger()
                .SetDefaultUserCredentials(StaticData.EventStoreUserCredentials)
                .Build();
        }

        public static void Run(IAnabasisActor statelessActor)
        {
            var rand = new Random();
            var position = 0;

            while (true)
            {
                if (position == 0)
                {
                    Console.WriteLine("Press enter to generate random event...");
                }

                Console.ReadLine();

                var entity = rand.Next(0, 3);

                string target;
                //if (entity == 1)
                    target = StaticData.EntityOne;
                //else if (entity == 2)
                //    target = StaticData.EntityTwo;
                //else
                //    target = StaticData.EntityThree;



                if (rand.Next(0, 2) == 1)
                {
                    Console.WriteLine($"Generating {nameof(EventCountOne)} for stream {target}");
                    statelessActor.EmitEventStore(new EventCountOne(position++, target, Guid.NewGuid())).Wait();
                }

                else
                {
                    Console.WriteLine($"Generating {nameof(EventCountTwo)} for stream {target}");
                    statelessActor.EmitEventStore(new EventCountTwo(position++, target, Guid.NewGuid())).Wait();
                }
            }
        }

        public static void Run<TConfiguration>(params IStatefulActor<EventCountAggregate, TConfiguration>[] statefulActors)
            where TConfiguration : IAggregateCacheConfiguration
        {
            foreach(var statefulActor in statefulActors)
            {
                statefulActor.AsObservableCache().Connect().Subscribe((changes) =>
                {
                    foreach (var change in changes)
                    {
                        change.Current.PrintConsole();
                    }
                });
            }

            Run(statefulActors.First() as IAnabasisActor);

        }
    }
}
