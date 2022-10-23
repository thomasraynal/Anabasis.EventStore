using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Insights
{
    public static class TracingAttributes
    {
        public static readonly string TraceId = "anabasis.traceid";
        public static readonly string CorrelationId = "anabasis.correlationid";
        public static readonly string MessageId = "anabasis.messageid";
        public static readonly string CauseId = "anabasis.causeid";
        public static readonly string RequestId = "anabasis.requestid";
        public static readonly string Application = "anabasis.app";
        public static readonly string Group = "anabasis.group";
        public static readonly string Environment = "anabasis.environment";
    }
}
