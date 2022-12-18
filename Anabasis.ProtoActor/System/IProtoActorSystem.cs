using Anabasis.Common;
using Proto;
using System;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.System
{
    public interface IProtoActorSystem : IDisposable
    {
        ActorSystem ActorSystem { get; }
        RootContext RootContext { get; }
        Task Send(IMessage[] messages, TimeSpan? timeout = null);
        Task Send(IMessage message, TimeSpan? timeout = null);
        Task ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false);
        TBus GetConnectedBus<TBus>() where TBus : class, IBus;
    }
}