using Anabasis.Common;
using Anabasis.ProtoActor.MessageHandlerActor;

namespace Anabasis.ProtoActor.EventStore
{
    public interface IAggregateMessageHandlerActorConfiguration: IAggregateCacheConfiguration, IMessageHandlerActorConfiguration
    {
    }
}