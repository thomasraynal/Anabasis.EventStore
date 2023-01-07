using Anabasis.Common;
using Anabasis.ProtoActor.System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Tests
{
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
    public class FaultyBusOneMessage : IMessage
    {
        private readonly Subject<bool> _onAcknowledgeSubject;

        public FaultyBusOneMessage()
        {
            _onAcknowledgeSubject = new Subject<bool>();
        }

        public Guid? TraceId => Guid.NewGuid();

        public Guid MessageId => Guid.NewGuid();

        public IEvent Content => throw new InvalidOperationException("boom");

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

        public ConcurrentBag<IMessage> EmittedMessages { get; private set; } = new ConcurrentBag<IMessage>();

        public void Emit(IMessage busOneMessage)
        {
            EmittedMessages.Add(busOneMessage);
            foreach (var subscriber in _subscribers)
            {
                subscriber(new[] { busOneMessage });
            }
        }

        public void Emit(IMessage[] busOneMessages)
        {
            foreach(var busOneMessage in busOneMessages)
            {
                EmittedMessages.Add(busOneMessage);
            }
    
            foreach (var subscriber in _subscribers)
            {
                subscriber(busOneMessages);
            }
        }
    }
}
