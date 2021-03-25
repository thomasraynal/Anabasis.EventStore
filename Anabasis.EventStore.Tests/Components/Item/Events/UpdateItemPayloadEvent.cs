using Anabasis.Tests;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Tests.Demo
{
    [ProtoContract]
    public class UpdateItemPayloadEvent : ProtoBuffEventBase<Guid, Item>
    {
        [ProtoMember(3)]
        public string Payload { get; set; }

        protected override void ApplyInternal(Item entity)
        {
            entity.State = ItemState.Ready;
            entity.Payload = Payload;
        }
    }
}
