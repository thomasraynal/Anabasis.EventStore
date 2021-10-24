using EventStore.ClientAPI.SystemData;
using Lamar;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Samples
{
    public class DemoSystemRegistry : ServiceRegistry
    {
        public DemoSystemRegistry()
        {
        }
    }

    public static class StaticData
    {
        public static readonly string GroupIdOne = "groupIdOne";
        public static readonly string GroupIdTwo = "groupIdTwo";

        public static readonly string PersistentStreamOne = "persistentStreamOne";
        public static readonly string PersistentStreamTwo = "persistentStreamTwo";

        public static readonly string EntityOne = "entityOne";
        public static readonly string EntityTwo = "entityTwo";
        public static readonly string EntityThree = "entityThree";

        public static readonly string Persisten = "entityThree";

        public static UserCredentials EventStoreUserCredentials = new UserCredentials("admin", "changeit");

        public static string EventStoreUrl => "tcp://localhost:1113";

    }
}
