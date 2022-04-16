using Anabasis.RabbitMQ.Event;
using System;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    public class TestEventZero : BaseRabbitMqEvent
    {
        public TestEventZero(Guid eventID, Guid correlationId) : base(eventID, eventID, correlationId)
        {
        }
    }

    public class TestEventOne : BaseRabbitMqEvent
    {
        public TestEventOne(Guid eventID, Guid correlationId) : base(eventID, eventID, correlationId)
        {
        }

        [RoutingPosition(0)]
        public string Identity => "one";

        [RoutingPosition(1)]
        public string FilterOne { get; set; }
    }

    public class TestEventTwo : BaseRabbitMqEvent
    {
        public TestEventTwo(Guid eventID, Guid correlationId) : base(eventID, eventID, correlationId)
        {
        }

        [RoutingPosition(0)]
        public string Identity => "two";

        [RoutingPosition(1)]
        public string FilterOne { get; set; }

        [RoutingPosition(2)]
        public string FilterTwo { get; set; }
    }

    public class TestEventTwoBis : BaseRabbitMqEvent
    {
        public TestEventTwoBis(Guid eventID, Guid correlationId) : base(eventID, eventID, correlationId)
        {
        }

        [RoutingPosition(0)]
        public string Identity => "two";

        [RoutingPosition(1)]
        public string SecondIdentity => "one";
    }
}
