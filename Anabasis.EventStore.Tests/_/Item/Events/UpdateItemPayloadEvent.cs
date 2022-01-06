using ProtoBuf;
using System;

namespace Anabasis.EventStore.Tests.Demo
{
    [ProtoContract]
    public class UpdateItemPayloadEvent : ProtoBuffEventBase<Item>
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
