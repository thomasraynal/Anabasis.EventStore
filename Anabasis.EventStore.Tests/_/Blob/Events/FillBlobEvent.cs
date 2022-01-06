using ProtoBuf;
using System;

namespace Anabasis.EventStore.Tests.Demo
{
    [ProtoContract]
    public class FillBlobEvent : ProtoBuffEventBase<Blob>
    {
        protected override void ApplyInternal(Blob entity)
        {
            entity.State = BlobState.Filled;
        }
    }
}
