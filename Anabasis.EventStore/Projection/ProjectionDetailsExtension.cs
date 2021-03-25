using EventStore.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Projection
{
  public static class ProjectionDetailsExtension
  {
    public static bool IsCompletedWithResults(this ProjectionDetails details)
    {
      var status = details.Status;
      return status.StartsWith("Completed", StringComparison.InvariantCultureIgnoreCase)
          && details.Status.EndsWith("Writing results", StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool IsRunningWithResults(this ProjectionDetails details)
    {
      var status = details.Status;
      return status.StartsWith("Running", StringComparison.InvariantCultureIgnoreCase)
          && details.Status.EndsWith("Writing results", StringComparison.InvariantCultureIgnoreCase);
    }

  }
}
