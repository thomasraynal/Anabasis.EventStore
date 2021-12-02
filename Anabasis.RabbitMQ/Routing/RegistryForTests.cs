using Microsoft.Extensions.Logging;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class RegistryForTests : Registry
    {
        public RegistryForTests()
        {
            For<ILogger>().Use<LoggerForTests>();
            For<ISerializer>().Use<JsonNetSerializer>();

            Scan((scanner) =>
            {
                scanner.AssembliesAndExecutablesFromApplicationBaseDirectory();
                scanner.WithDefaultConventions();
            });
        }
    }
}
