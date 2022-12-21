using Anabasis.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Proto;
using System;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.System
{
    public interface IProtoActorSystem : IDisposable, IHealthCheck
    {
        string Id { get; }

        public ActorSystem ActorSystem { get; }
        public RootContext RootContext { get; }

        Task ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false);
        PID[] CreateActors<TActor>(int instanceCount, Action<Props>? onCreateProps = null) where TActor : IActor;
        PID CreateConsistentHashPool<TActor>(int poolSize, int replicaCount = 100, Action<Props>? onCreateProps = null, Func<string, uint>? hash = null, Func<object, string>? messageHasher = null) where TActor : IActor;
        PID CreateRoundRobinPool<TActor>(int poolSize, Action<Props>? onCreateProps = null) where TActor : IActor;
        TBus GetConnectedBus<TBus>() where TBus : class, IBus;
        Task Send(IMessage message, TimeSpan? timeout = null);
        Task Send(IMessage[] messages, TimeSpan? timeout = null);
    }
}