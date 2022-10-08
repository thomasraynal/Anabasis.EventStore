using Anabasis.Common;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Cache
{
    public class SingleStreamCatchupCacheConfiguration : MultipleStreamsCatchupCacheConfiguration
    {
        public SingleStreamCatchupCacheConfiguration(string streamId, UserCredentials? userCredentials = null) : base(streamId)
        {
            UserCredentials = userCredentials;
        }
    }
}
