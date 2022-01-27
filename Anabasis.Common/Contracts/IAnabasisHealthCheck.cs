using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface IAnabasisHealthCheck
    {
        HealthStatus HealthStatus { get; }
        string[] HealthMessages { get; }
    }
}
