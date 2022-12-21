﻿using Anabasis.Common;
using Anabasis.ProtoActor.MessageBufferActor;
using Anabasis.ProtoActor.MessageHandlerActor;
using Anabasis.ProtoActor.Queue;
using Anabasis.ProtoActor.System;
using Lamar;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Proto;
using Proto.DependencyInjection;
using Proto.Mailbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Tests
{

    public class DummyLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return Disposable.Empty;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Debug.WriteLine(state);
        }
    }

    public class DummyLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DummyLogger();
        }

        public void Dispose()
        {
        }
    }



    public static class BusOneExtension
    {
        public static void SubscribeToBusOne(this IProtoActorSystem protoActorSystem)
        {

            var busOne = protoActorSystem.GetConnectedBus<BusOne>();

            busOne.Subscribe(async (messages) =>
            {
                await protoActorSystem.Send(messages);

            });
        }
    }

    public class BusOneMessage : IMessage
    {
        private readonly Subject<bool> _onAcknowledgeSubject;

        public BusOneMessage(IEvent @event)
        {
            Content = @event;
            _onAcknowledgeSubject = new Subject<bool>();
        }

        public Guid? TraceId => Guid.NewGuid();

        public Guid MessageId => Guid.NewGuid();

        public IEvent Content { get; }

        public bool IsAcknowledged { get; private set; }

        public IObservable<bool> OnAcknowledged => _onAcknowledgeSubject;

        public Task Acknowledge()
        {
            IsAcknowledged = true;

            _onAcknowledgeSubject.OnNext(true);
            _onAcknowledgeSubject.OnCompleted();

            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string? reason = null)
        {

            _onAcknowledgeSubject.OnNext(false);
            _onAcknowledgeSubject.OnCompleted();
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

        public void Emit(IMessage busOneMessage)
        {

            foreach (var subscriber in _subscribers)
            {
                subscriber(new[] { busOneMessage });
            }
        }

        public void Emit(IMessage[] busOneMessages)
        {

            EmittedMessages.AddRange(busOneMessages);

            foreach (var subscriber in _subscribers)
            {
                subscriber(busOneMessages);
            }
        }
    }

    [TestFixture]
    public class ProtoActorTests
    {

        [Test]
        public async Task ShouldCreateAnActor()
        {
            var container = GetContainer();

            var actorSystem = new ActorSystem().WithServiceProvider(container);

            var killSwitch = Substitute.For<IKillSwitch>();
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);
            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(int.MaxValue, true);

            var protoActoSystem = new ProtoActorSystem(supervisorStrategy,
              protoActorPoolDispatchQueueConfiguration,
              container.ServiceProvider,
              new DummyLoggerFactory());

            var pid = protoActoSystem.CreateActors<TestActor>(1);

            var busOne = new BusOne();

            await protoActoSystem.ConnectTo(busOne);

            protoActoSystem.SubscribeToBusOne();

            for (var i = 0; i < 100; i++)
            {
                busOne.Emit(new BusOneMessage(new BusOneEvent(i)));
            }

            await Task.Delay(5000);

        }

        private Container GetContainer()
        {
            var container = new Lamar.Container(serviceRegistry =>
            {
                serviceRegistry.For<TestActor>().Use<TestActor>();
                serviceRegistry.For<TestMessageBufferActor>().Use<TestMessageBufferActor>();
                serviceRegistry.For<ILoggerFactory>().Use<DummyLoggerFactory>();
                serviceRegistry.For<IMessageHandlerActorConfiguration>().Use<MessageHandlerActorConfiguration>();
                serviceRegistry.For<IMessageBufferActorConfiguration>().Use((_) =>
                {
                    var messageBufferActorConfiguration = new MessageBufferActorConfiguration(TimeSpan.FromSeconds(1), bufferingStrategies: new IBufferingStrategy[]
                    {
                        new AbsoluteTimeoutBufferingStrategy(TimeSpan.FromSeconds(1)),
                        new BufferSizeBufferingStrategy(5)
                    });

                    return messageBufferActorConfiguration;
                });
            });

            return container;
        }

        [Test]
        public async Task ShouldStopAnActorSystem()
        {
            var container = GetContainer();

            var actorSystem = new ActorSystem().WithServiceProvider(container);

            var props = actorSystem.DI().PropsFor<TestMessageBufferActor>().WithMailbox(() => UnboundedMailbox.Create());


            var killSwitch = Substitute.For<IKillSwitch>();
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);

            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(int.MaxValue, true);

            var protoActorSystem = new ProtoActorSystem(supervisorStrategy,
                    protoActorPoolDispatchQueueConfiguration,
                    container.ServiceProvider,
                    new DummyLoggerFactory());

            var pid = protoActorSystem.CreateActors<TestActor>(1);


            protoActorSystem.Dispose();

            await Task.Delay(1000);

        }


            [Test]
        public async Task ShouldCreateAnActorPool()
        {

            var container = GetContainer();

            var actorSystem = new ActorSystem().WithServiceProvider(container);

            var props = actorSystem.DI().PropsFor<TestMessageBufferActor>().WithMailbox(() => UnboundedMailbox.Create());


            var killSwitch = Substitute.For<IKillSwitch>();
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);

            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(int.MaxValue, true);

            var protoActoSystem = new ProtoActorSystem(supervisorStrategy, 
                    protoActorPoolDispatchQueueConfiguration, 
                    container.ServiceProvider,
                    new DummyLoggerFactory());

            var ruondRobinPool = protoActoSystem.CreateRoundRobinPool<TestMessageBufferActor>(5);

            var busOne = new BusOne();

            await protoActoSystem.ConnectTo(busOne);

            protoActoSystem.SubscribeToBusOne();

            var rand = new Random();

            for (var i = 0; i < 50; i++)
            {
                busOne.Emit(Enumerable.Range(0,rand.Next(1,10)).Select(_=> new BusOneMessage(new BusOneEvent(i))).ToArray());
            }

            await Task.Delay(6000);

            for (var i = 0; i < 50; i++)
            {
                busOne.Emit(Enumerable.Range(0, rand.Next(1, 10)).Select(_ => new BusOneMessage(new BusOneEvent(i))).ToArray());
            }

            await Task.Delay(6000);

            var unackedMessages = busOne.EmittedMessages.Where(message => !message.IsAcknowledged).ToArray();

        }

    }
}