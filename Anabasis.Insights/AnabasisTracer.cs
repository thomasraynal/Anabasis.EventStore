using Anabasis.Common;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Anabasis.Insights
{
    public class AnabasisTracer : ITracer
    {
        private readonly Tracer _tracer;
        private readonly AnabasisAppContext _anabasisAppContext;

        public AnabasisTracer(Tracer tracer, AnabasisAppContext anabasisAppContext)
        {
            _tracer = tracer;
            _anabasisAppContext = anabasisAppContext;
        }

        private SpanAttributes CreateSpanAttributes(Guid traceId,
            Guid? correlationId = null,
            Guid? requestId = null,
            Guid? causeId = null,
            Guid? messageId = null,
            SpanAttributes? initialAttributes = null)
        {
            initialAttributes ??= new SpanAttributes();

            initialAttributes.Add(TracingAttributes.TraceId, $"{traceId}");

            initialAttributes.Add(TracingAttributes.Application, _anabasisAppContext.ApplicationName);
            initialAttributes.Add(TracingAttributes.Group, _anabasisAppContext.ApplicationGroup);
            initialAttributes.Add(TracingAttributes.Environment, $"{_anabasisAppContext.Environment}");

            if (null != correlationId)
            {
                initialAttributes.Add(TracingAttributes.CorrelationId, $"{correlationId}");
            }

            if (null != requestId)
            {
                initialAttributes.Add(TracingAttributes.RequestId, $"{requestId}");
            }

            if (null != causeId)
            {
                initialAttributes.Add(TracingAttributes.CauseId, $"{causeId}");
            }

            if (null != messageId)
            {
                initialAttributes.Add(TracingAttributes.MessageId, $"{messageId}");
            }

            return initialAttributes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpan StartActiveSpan(string name,
            SpanKind kind,
            in TelemetrySpan parentSpan,
            Guid traceId,
            Guid? correlationId = null,
            Guid? requestId = null,
            Guid? causeId = null,
            Guid? messageId = null,
            SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default)
        {
            if (traceId == default)
            {
                throw new ArgumentNullException($"{traceId}");
            }

            initialAttributes = CreateSpanAttributes(
                traceId,
                correlationId,
                requestId,
                causeId,
                messageId,
                initialAttributes);

            var telemetrySpan = _tracer.StartActiveSpan(name, kind, in parentSpan, initialAttributes, links, startTime);

            return telemetrySpan;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpan StartActiveSpan(string name,
            SpanKind kind,
            in TelemetrySpan parentSpan,
            IEvent @event,
            SpanAttributes? initialAttributes = null,
            IEnumerable<Link>? links = null,
            DateTimeOffset startTime = default)
        {

            if (null == @event.TraceId)
            {
                throw new InvalidOperationException("A traceId must be specified");
            }

            initialAttributes = CreateSpanAttributes(
                @event.TraceId.Value,
                @event.CorrelationId,
                null,
                @event.CauseId,
                null,
                initialAttributes);

            return _tracer.StartActiveSpan(name, kind, in parentSpan, initialAttributes, links, startTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpan StartActiveSpan(string name, SpanKind kind, in TelemetrySpan parentSpan, IMessage message, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default)
        {
            if (null == message.TraceId)
            {
                throw new InvalidOperationException("A traceId must be specified");
            }

            initialAttributes = CreateSpanAttributes(
                message.TraceId.Value,
                null,
                null,
                null,
                message.MessageId,
                initialAttributes);

            return _tracer.StartActiveSpan(name, kind, in parentSpan, initialAttributes, links, startTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpan StartActiveSpan(string name,
            Guid traceId,
            Guid? correlationId = null,
            Guid? requestId = null,
            Guid? causeId = null,
            Guid? messageId = null,
            SpanKind kind = SpanKind.Internal, in SpanContext parentContext = default, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default)
        {
            initialAttributes = CreateSpanAttributes(
                traceId,
                correlationId,
                requestId,
                causeId,
                messageId,
                initialAttributes);

            return _tracer.StartActiveSpan(name, kind, in parentContext, initialAttributes, links, startTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpan StartRootSpan(string name,
            Guid traceId,
            Guid? correlationId = null,
            Guid? requestId = null,
            Guid? causeId = null,
            Guid? messageId = null,
            SpanKind kind = SpanKind.Internal, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default)
        {
            initialAttributes = CreateSpanAttributes(
                traceId,
                correlationId,
                requestId,
                causeId,
                messageId,
                initialAttributes);

            return _tracer.StartRootSpan(name, kind, initialAttributes, links, startTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpan StartSpan(string name,
            SpanKind kind,
            in TelemetrySpan parentSpan,
            Guid traceId,
            Guid? correlationId = null,
            Guid? requestId = null,
            Guid? causeId = null,
            Guid? messageId = null,
            SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default)
        {
            initialAttributes = CreateSpanAttributes(
                traceId,
                correlationId,
                requestId,
                causeId,
                messageId,
                initialAttributes);

            return _tracer.StartSpan(name, kind, parentSpan, initialAttributes, links, startTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpan StartSpan(string name,
            Guid traceId,
            Guid? correlationId = null,
            Guid? requestId = null,
            Guid? causeId = null,
            Guid? messageId = null,
            SpanKind kind = SpanKind.Internal, in SpanContext parentContext = default, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default)
        {
            initialAttributes = CreateSpanAttributes(
            traceId,
            correlationId,
            requestId,
            causeId,
            messageId,
            initialAttributes);

            return _tracer.StartSpan(name, kind, parentContext, initialAttributes, links, startTime);
        }


    }
}
