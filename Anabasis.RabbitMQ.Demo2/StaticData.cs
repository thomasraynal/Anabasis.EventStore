using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Demo
{
    public static class StaticData
    {

        public static UserCredentials UserCredentials => new("admin", "changeit");

        public static string[] ProductIds => new [] { "product1", "product2", "product3", "product4" };
        public static string ProducyInventoryExchange = "inventory";
    }
}
