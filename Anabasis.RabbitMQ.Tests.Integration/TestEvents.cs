using RabbitMQPlayground.Routing;
using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    public class TestEventZero : BaseRabbitMqEvent
    {
        public TestEventZero(Guid eventID, Guid correlationId) : base(eventID, correlationId)
        {
        }
    }

    public class TestEventOne : BaseRabbitMqEvent
    {
        public TestEventOne(Guid eventID, Guid correlationId) : base(eventID, correlationId)
        {
        }

        [RoutingPosition(0)]
        public string Identity => "one";

        [RoutingPosition(1)]
        public string Data { get; set; }
    }

    public class TestEventTwo : BaseRabbitMqEvent
    {
        public TestEventTwo(Guid eventID, Guid correlationId) : base(eventID, correlationId)
        {
        }

        [RoutingPosition(0)]
        public string Identity => "two";

        [RoutingPosition(1)]
        public string Data { get; set; }

        [RoutingPosition(2)]
        public string Data2 { get; set; }
    }

    public class TestEventTwoBis : BaseRabbitMqEvent
    {
        public TestEventTwoBis(Guid eventID, Guid correlationId) : base(eventID, correlationId)
        {
        }

        [RoutingPosition(0)]
        public string Identity => "two";


        [RoutingPosition(1)]
        public string SecondIdentity => "one";
    }
}
