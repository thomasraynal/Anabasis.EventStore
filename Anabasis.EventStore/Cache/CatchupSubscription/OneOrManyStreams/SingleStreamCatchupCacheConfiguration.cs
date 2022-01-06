using Anabasis.Common;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Cache
{
    public class SingleStreamCatchupCacheConfiguration< TAggregate> : MultipleStreamsCatchupCacheConfiguration< TAggregate> where TAggregate : IAggregate
    {
        public SingleStreamCatchupCacheConfiguration(string streamId, UserCredentials userCredentials = null) : base(streamId)
        {
            UserCredentials = userCredentials;
        }
    }
}
