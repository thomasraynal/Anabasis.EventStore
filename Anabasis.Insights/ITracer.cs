using Anabasis.Common;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;

namespace Anabasis.Insights
{
    public interface ITracer
    {
        TelemetrySpan StartActiveSpan(string name, Guid traceId, Guid? correlationId = null, Guid? requestId = null, Guid? causeId = null, Guid? messageId = null, SpanKind kind = SpanKind.Internal, in SpanContext parentContext = default, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default);
        TelemetrySpan StartActiveSpan(string name, SpanKind kind, in TelemetrySpan parentSpan, Guid traceId, Guid? correlationId = null, Guid? requestId = null, Guid? causeId = null, Guid? messageId = null, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default);
        TelemetrySpan StartActiveSpan(string name, SpanKind kind, in TelemetrySpan parentSpan, IEvent @event, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default);
        TelemetrySpan StartActiveSpan(string name, SpanKind kind, in TelemetrySpan parentSpan, IMessage message, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default);
        TelemetrySpan StartRootSpan(string name, Guid traceId, Guid? correlationId = null, Guid? requestId = null, Guid? causeId = null, Guid? messageId = null, SpanKind kind = SpanKind.Internal, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default);
        TelemetrySpan StartSpan(string name, Guid traceId, Guid? correlationId = null, Guid? requestId = null, Guid? causeId = null, Guid? messageId = null, SpanKind kind = SpanKind.Internal, in SpanContext parentContext = default, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default);
        TelemetrySpan StartSpan(string name, SpanKind kind, in TelemetrySpan parentSpan, Guid traceId, Guid? correlationId = null, Guid? requestId = null, Guid? causeId = null, Guid? messageId = null, SpanAttributes? initialAttributes = null, IEnumerable<Link>? links = null, DateTimeOffset startTime = default);
    }
}