using Anabasis.Common;
using Proto;

namespace Anabasis.ProtoActor
{
    public interface IProtoActorSystem : IDisposable
    {
        ActorSystem ActorSystem { get; }
        RootContext RootContext { get; }
        Task OnMessageReceived(IMessage message);
        Task ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false);
        TBus GetConnectedBus<TBus>() where TBus : class, IBus;
    }
}