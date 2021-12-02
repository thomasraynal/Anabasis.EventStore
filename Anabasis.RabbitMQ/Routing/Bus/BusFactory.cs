using RabbitMQ.Client;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public static class BusFactory
    {
        public static IContainer CreateContainer<TRegistry>(IConnection connection, IBusConfiguration busConfiguration) where TRegistry : Registry, new()
        {
            return new Container(configuration =>
            {
                configuration.AddRegistry<TRegistry>();
                configuration.For<IConnection>().Use(connection);
                configuration.For<IBus>().Use<Bus>().Singleton();
                configuration.For<IBusConfiguration>().Use(busConfiguration);
                configuration.For<IEventSerializer>().Use<EventSerializer>();
            });

        }

        public static IBus CreateBus<TRegistry>(IConnection connection, IBusConfiguration busConfiguration) where TRegistry : Registry, new()
        {
            var container = CreateContainer<TRegistry>(connection, busConfiguration);

            return container.GetInstance<IBus>();
        }

    }
}
