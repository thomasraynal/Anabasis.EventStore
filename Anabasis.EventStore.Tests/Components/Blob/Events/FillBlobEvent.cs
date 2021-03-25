using ProtoBuf;
using System;

namespace Anabasis.EventStore.Tests.Demo
{
    [ProtoContract]
    public class FillBlobEvent : ProtoBuffEventBase<Guid, Blob>
    {
        protected override void ApplyInternal(Blob entity)
        {
            entity.State = BlobState.Filled;
        }
    }
}
