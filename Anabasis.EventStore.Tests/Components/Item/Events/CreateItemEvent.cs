using Anabasis.Tests;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Tests.Demo
{
    [ProtoContract]
    public class CreateItemEvent : ProtoBuffEventBase<Guid,Item>
    {
        protected override void ApplyInternal(Item entity)
        {
            entity.State = ItemState.Created;
            entity.Payload = "NONE";
        }
    }
}
