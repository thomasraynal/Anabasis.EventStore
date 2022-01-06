using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Tests.Demo
{
    [ProtoContract]
    public class CreateItemEvent : ProtoBuffEventBase<Item>
    {
        protected override void ApplyInternal(Item entity)
        {
            entity.State = ItemState.Created;
            entity.Payload = "NONE";
        }
    }
}
