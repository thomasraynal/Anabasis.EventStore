using Anabasis.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Proto;
using Proto.DependencyInjection;
using Proto.Mailbox;
using Proto.Router;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;

namespace Anabasis.ProtoActor.Tests
{
    public class TestActor : MessageBufferActorBase
    {
        public TestActor(MessageBufferActorConfiguration messageBufferActorConfiguration, ILoggerFactory? loggerFactory = null) : base(messageBufferActorConfiguration, loggerFactory)
        {
        }

        public override async Task ReceiveAsync(object[] messages, IContext context)
        {

            var rand = new Random();

            await Task.Delay(rand.Next(100, 500));

            Debug.WriteLine($"{context.Self.Id} handle batch of {messages.Length} messages");

            foreach (var message in messages.Cast<IMessage>())
            {
                await message.Acknowledge();
            }

        }
    }

    public static class BusOneExtension
    {
        public static void SubscribeToBusOne(this IProtoActorPoolSystem protoActorSystem)
        {

            var busOne = protoActorSystem.GetConnectedBus<BusOne>();

            busOne.Subscribe(async (messages) =>
            {
                //Debug.WriteLine($"Process {message.GetType()}");

                await protoActorSystem.Send(messages);

            });
        }
    }

    public class BusOneMessage : IMessage
    {
        public BusOneMessage(IEvent @event)
        {
            Content = @event;
        }

        public Guid? TraceId => Guid.NewGuid();

        public Guid MessageId => Guid.NewGuid();

        public IEvent Content { get; }

        public bool IsAcknowledged { get; private set; }

        public Task Acknowledge()
        {
            IsAcknowledged = true;

            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string? reason = null)
        {
            return Task.CompletedTask;
        }
    }

    public class BusOne : IBus
    {
        private readonly List<Func<IMessage[], Task>> _subscribers = new();

        public string BusId => Guid.NewGuid().ToString();

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Subscribe(Func<IMessage[], Task> onMessageReceived)
        {
            _subscribers.Add(onMessageReceived);
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        public List<IMessage> EmittedMessages { get; private set; } = new List<IMessage>();

        public void Emit(IMessage[] busOneMessage)
        {

            EmittedMessages.AddRange(busOneMessage);

            foreach (var subscriber in _subscribers)
            {
                subscriber(busOneMessage);
            }
        }
    }


    //use lamar
    // handle https://proto.actor/docs/receive-timeout/

    [TestFixture]
    public class ProtoActorTests
    {

        [Test]
        public async Task ShouldCreateAnActor()
        {

        }


        [Test]
        public async Task ShouldCreateAnActorPool()
        {
           
            var container = new Lamar.Container(serviceRegistry =>
            {
                serviceRegistry.For<TestActor>().Use<TestActor>();

                serviceRegistry.For<MessageBufferActorConfiguration>().Use((_) =>
                {
                    var messageBufferActorConfiguration = new MessageBufferActorConfiguration(TimeSpan.FromSeconds(1), new IBufferingStrategy[]
                    {
                        new AbsoluteTimeoutBufferingStrategy(TimeSpan.FromSeconds(1)),
                        new BufferSizeBufferingStrategy(5)
                    });

                    return messageBufferActorConfiguration;
                });
            });


            var actorSystem = new ActorSystem().WithServiceProvider(container);

            var props = actorSystem.DI().PropsFor<TestActor>().WithMailbox(() => UnboundedMailbox.Create()).WithMailbox(() => UnboundedMailbox.Create());


            var killSwitch = Substitute.For<IKillSwitch>();
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);

            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(100, true);

            var protoActorPoolSystem = new ProtoActorPoolSystem(supervisorStrategy, 
                protoActorPoolDispatchQueueConfiguration, 
                container.ServiceProvider);

            var ruondRobinPool = protoActorPoolSystem.CreateRoundRobinPool<TestActor>(5);

            var busOne = new BusOne();

            await protoActorPoolSystem.ConnectTo(busOne);

            protoActorPoolSystem.SubscribeToBusOne();

            var rand = new Random();

            for (var i = 0; i < 100; i++)
            {
                busOne.Emit(Enumerable.Range(0,rand.Next(1,10)).Select(_=> new BusOneMessage(new BusOneEvent())).ToArray());
            }

            await Task.Delay(10000);

            var unackedMessages = busOne.EmittedMessages.Where(message => !message.IsAcknowledged).ToArray();

        }

    }
}