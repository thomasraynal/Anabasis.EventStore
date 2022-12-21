using Anabasis.Common;
using Anabasis.ProtoActor.MessageBufferActor;
using Anabasis.ProtoActor.MessageHandlerActor;
using Anabasis.ProtoActor.Queue;
using Anabasis.ProtoActor.System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using System;
using System.Collections.Generic;

namespace Anabasis.ProtoActor.AspNet
{
    public class ProtoActorSystemBuilder
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly List<Action<IProtoActorSystem>> _onCreateProtoActorSystem;

        public ProtoActorSystemBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
            _onCreateProtoActorSystem = new List<Action<IProtoActorSystem>>();

            _serviceCollection.AddSingleton(serviceProvider => serviceProvider);
        }

        public ProtoActorSystemBuilder WithDefaultSupervisionStrategies()
        {
            var killSwitch = new KillSwitch();
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);

            _serviceCollection.AddSingleton<IKillSwitch>(killSwitch);
            _serviceCollection.AddSingleton<ISupervisorStrategy>(supervisorStrategy);

            return this;
        }

        public ProtoActorSystemBuilder WithDefaultDispatchQueueConfiguration(int? bufferMaxSize = null)
        {
            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(bufferMaxSize ?? int.MaxValue, true);
            _serviceCollection.AddSingleton<IProtoActorPoolDispatchQueueConfiguration>(protoActorPoolDispatchQueueConfiguration);

            return this;
        }

        public ProtoActorSystemBuilder WithMessageHandlerActorConfiguration(bool swallowUnkwownEvents = true,
           TimeSpan? idleTimeoutFrequency = null)
        {
            var messageHandlerActorConfiguration = new MessageHandlerActorConfiguration(
               swallowUnkwownEvents: swallowUnkwownEvents,
               idleTimeoutFrequency: idleTimeoutFrequency);

            _serviceCollection.AddSingleton<IMessageHandlerActorConfiguration>(messageHandlerActorConfiguration);

            return this;
        }

        public ProtoActorSystemBuilder WithMessageBufferHandlerActorConfiguration(int? bufferMaxSize, 
            bool swallowUnkwownEvents = true,
            TimeSpan? absoluteBufferConsumptionTimeout = null, 
            TimeSpan? reminderSchedulingDelay = null, 
            TimeSpan? idleTimeoutFrequency = null)
        {
            var messageBufferActorConfiguration = new MessageBufferActorConfiguration(
               reminderSchedulingDelay: reminderSchedulingDelay ?? TimeSpan.FromSeconds(1),
               swallowUnkwownEvents: swallowUnkwownEvents,
               idleTimeoutFrequency: idleTimeoutFrequency,
               bufferingStrategies: new IBufferingStrategy[]
              {
                    new AbsoluteTimeoutBufferingStrategy(absoluteBufferConsumptionTimeout ?? TimeSpan.FromSeconds(1)),
                    new BufferSizeBufferingStrategy(bufferMaxSize ?? 5)
              });

            _serviceCollection.AddSingleton<IMessageBufferActorConfiguration>(messageBufferActorConfiguration);

            return this;
        }

        public ProtoActorSystemBuilder AddRoundRobinPool<TActor, TActorConfiguration>(TActorConfiguration actorConfiguration, int poolSize, Action<Props>? onCreateProps = null)
            where TActor : class, IActor
            where TActorConfiguration : class, IMessageHandlerActorConfiguration
        {
            _serviceCollection.AddTransient<TActor>();
            _serviceCollection.AddSingleton(actorConfiguration);

            _onCreateProtoActorSystem.Add((protoactorSystem) =>
            {
                protoactorSystem.CreateRoundRobinPool<TActor>(poolSize, onCreateProps);

            });

            return this;
        }

        public ProtoActorSystemBuilder AddConsistentHashPool<TActor, TActorConfiguration>(TActorConfiguration actorConfiguration, int poolSize, int replicaCount = 100, Action<Props>? onCreateProps = null)
            where TActor : class, IActor
            where TActorConfiguration : class, IMessageHandlerActorConfiguration
        {
            _serviceCollection.AddTransient<TActor>();
            _serviceCollection.AddSingleton(actorConfiguration);

            _onCreateProtoActorSystem.Add((protoactorSystem) =>
            {
                protoactorSystem.CreateConsistentHashPool<TActor>(poolSize, replicaCount, onCreateProps);

            });

            return this;
        }

        public ProtoActorSystemBuilder AddStatelessActors<TActor, TActorConfiguration>(TActorConfiguration actorConfiguration, int instanceCount = 1, Action<Props>? onCreateProps = null)
            where TActor : class, IActor
            where TActorConfiguration : class, IMessageHandlerActorConfiguration
        {
            _serviceCollection.AddTransient<TActor>();
            _serviceCollection.AddSingleton(actorConfiguration);

            _onCreateProtoActorSystem.Add((protoactorSystem) =>
            {
                protoactorSystem.CreateActors<TActor>(instanceCount, onCreateProps);

            });

            return this;
        }

        public void Create(IApplicationBuilder applicationBuilder)
        {
            var protoActorSystem = applicationBuilder.ApplicationServices.GetService(typeof(IProtoActorSystem));

            if (null == protoActorSystem)
            {
                throw new InvalidOperationException($"No {nameof(IProtoActorSystem)} registered");
            }

            foreach (var onCreate in _onCreateProtoActorSystem)
            {
                onCreate((IProtoActorSystem)protoActorSystem);
            }
        }
    }
}
