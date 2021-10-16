using Anabasis.EventStore.Shared;
using System;

namespace Anabasis.EventStore.Tests.Demo
{
    public class Item : BaseAggregate<Guid>
    {
        public Item()
        {
            EntityId = Guid.NewGuid();
        }

        public override bool Equals(object obj)
        {
            return obj is Item && obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return EntityId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{EntityId} - {State} - {Payload}";
        }

        public string Payload { get; set; }

        public ItemState State { get; set; }

    }
}
