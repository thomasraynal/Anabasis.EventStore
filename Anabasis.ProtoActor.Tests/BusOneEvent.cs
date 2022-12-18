using Anabasis.Common;
using System;

namespace Anabasis.ProtoActor.Tests
{
    
    public class BusOneEvent : IEvent
    {
        
        public BusOneEvent(int eventNumber)
        {
            this.EventNumber = eventNumber;
        }

        public int EventNumber { get; }

        public Guid? TraceId => Guid.NewGuid();

        public Guid EventId => Guid.NewGuid();

        public Guid CorrelationId => Guid.NewGuid();

        public Guid? CauseId => Guid.NewGuid();

        public string EventName => throw new NotImplementedException();

        public bool IsCommand => false;

        public DateTime Timestamp => DateTime.UtcNow;

        public bool IsAggregateEvent => false;

        public string? EntityId => $"{Guid.NewGuid()}";
    }
}