using System;

namespace Anabasis.Common.Events
{
  public class ErrorThrowed : BaseAnabasisExporterEvent
  {
    public ErrorThrowed(Guid correlationId, string streamId, Exception exception) : base(correlationId, streamId)
    {
      Exception = exception;
    }

    public Exception Exception { get; set; }

    public override string Log()
    {
      return $"{nameof(ErrorThrowed)} - {Exception}";
    }
  }
}
