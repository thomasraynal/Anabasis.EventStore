using Anabasis.EventStore.Shared;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Cache
{
    public class SingleStreamCatchupCacheConfiguration<TKey, TAggregate> : MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate> where TAggregate : IAggregate<TKey>
    {
        public SingleStreamCatchupCacheConfiguration(string streamId, UserCredentials userCredentials = null) : base(streamId)
        {
            UserCredentials = userCredentials;
        }
    }
}
