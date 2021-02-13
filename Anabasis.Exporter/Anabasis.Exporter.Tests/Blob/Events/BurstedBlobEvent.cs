using Anabasis.Tests;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using Anabasis.EventStore;

namespace Anabasis.Tests.Demo
{
    [ProtoContract]
    public class BurstedBlobEvent : ProtoBuffEventBase<Guid, Blob>
    {
        protected override void ApplyInternal(Blob entity)
        {
            entity.State = BlobState.Bursted;
        }
    }
}
