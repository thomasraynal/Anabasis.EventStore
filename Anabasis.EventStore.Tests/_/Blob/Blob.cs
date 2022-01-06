using Anabasis.EventStore;
using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Tests.Demo
{
    public class Blob : BaseAggregate
    {
        public Blob()
        {
            EntityId = $"{Guid.NewGuid()}";
        }

        public override bool Equals(object obj)
        {
            return obj is Blob && obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return EntityId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{EntityId} - {State}";
        }

        public BlobState State { get; set; }
        public String Payload { get; set; }
    }
}
