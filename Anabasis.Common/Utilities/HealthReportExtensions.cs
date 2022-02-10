using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.Common.Utilities
{
    public static class HealthReportExtensions
    {
        public static HealthReport Combine(this HealthReport hr, HealthReport healthReport)
        {
            var currentStatus = new[] { hr.Status, healthReport.Status }.Min();
            var entries = hr.Entries.Concat(healthReport.Entries).ToDictionary(kv => kv.Key, kv => kv.Value);
            var totalDuration = hr.TotalDuration + healthReport.TotalDuration;

            var combinedHealthReport = new HealthReport(entries, currentStatus, totalDuration);

            return combinedHealthReport;

        }
    }
}
