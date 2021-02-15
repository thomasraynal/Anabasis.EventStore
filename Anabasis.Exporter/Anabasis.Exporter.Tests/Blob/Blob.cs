using Anabasis.EventStore;
using Anabasis.Tests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Tests.Demo
{
    public class Blob : BaseAggregate<Guid>
    {
        public Blob()
        {
            EntityId = Guid.NewGuid();
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
