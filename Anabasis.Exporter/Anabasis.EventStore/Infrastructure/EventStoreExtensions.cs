using System;
using System.Text;
using EventStore.ClientAPI;

namespace Anabasis.EventStore
{
    public static class EventStoreExtensions
    {
        public static IMutable<TKey, TEntity> GetMutator<TKey, TEntity>(this RecordedEvent evt, Type type, ISerializer serializer) where TEntity : IAggregate<TKey>
        {
            return (IMutable<TKey, TEntity>)serializer.DeserializeObject(evt.Data, type);
        }
    }
}