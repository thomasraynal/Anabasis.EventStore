using Anabasis.Tests;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Tests.Demo
{
    [ProtoContract]
    public class DeleteItemEvent : ProtoBuffEventBase<Guid, Item>
    {
        protected override void ApplyInternal(Item entity)
        {
            entity.State = ItemState.Deleted;
        }
    }
}
