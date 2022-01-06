using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using Anabasis.EventStore;

namespace Anabasis.EventStore.Tests.Demo
{
    [ProtoContract]
    public class BurstedBlobEvent : ProtoBuffEventBase<Blob>
    {
        protected override void ApplyInternal(Blob entity)
        {
            entity.State = BlobState.Bursted;
        }
    }
}
