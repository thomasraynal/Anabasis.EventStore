using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Anabasis.Common.Actor
{
    [Serializable]
    public class BusUnhealthyException : Exception
    {
        public HealthCheckResult HealthCheckResult { get; }

        public BusUnhealthyException(HealthCheckResult healthCheckResult)
        {
            HealthCheckResult = healthCheckResult;
        }

        public BusUnhealthyException(string message, HealthCheckResult healthCheckResult) : base(message)
        {
            HealthCheckResult = healthCheckResult;
        }

        public BusUnhealthyException(string message, HealthCheckResult healthCheckResult, Exception innerException) : base(message, innerException)
        {
            HealthCheckResult = healthCheckResult;
        }

        protected BusUnhealthyException(HealthCheckResult healthCheckResult, SerializationInfo info, StreamingContext context) : base(info, context)
        {
            HealthCheckResult = healthCheckResult;
        }
    }
}
