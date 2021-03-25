using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using Anabasis.EventStore;
using Anabasis.EventStore.Shared;

namespace Anabasis.EventStore.Tests.Demo
{
  [ProtoContract]
  [ProtoInclude(10, typeof(CreateItemEvent))]
  [ProtoInclude(11, typeof(DeleteItemEvent))]
  [ProtoInclude(12, typeof(UpdateItemPayloadEvent))]
  [ProtoInclude(13, typeof(BurstedBlobEvent))]
  [ProtoInclude(14, typeof(FillBlobEvent))]
  public abstract class ProtoBuffEventBase<TKey, TEntity> : IMutable<TKey, TEntity> where TEntity : IAggregate<TKey>
  {
    protected ProtoBuffEventBase()
    {
    }

    [ProtoMember(1, IsRequired = true)]
    public string Name { get; set; }

    [ProtoMember(2, IsRequired = true)]
    public TKey EntityId { get; set; }

    protected abstract void ApplyInternal(TEntity entity);

    public void Apply(TEntity entity)
    {
      ApplyInternal(entity);
      entity.EntityId = EntityId;
    }

    public string StreamId
    {
      get
      {
        return EntityId.ToString();
      }
      set { }
    }
  }
}
