using Anabasis.Common.Events;
using System;

namespace Anabasis.Actor.Exporter.Events
{
  public class ErrorOccured : BaseAnabasisExporterEvent
  {
    public ErrorOccured(Guid correlationId,
      string streamId,
      string topicId,
      string caller,
      string message) : base(correlationId, streamId, topicId)
    {
      Caller = caller;
      Message = message;
    }

    public string Caller { get; set; }
    public string Message { get; set; }

    public override string Log()
    {
      return $"{nameof(ErrorOccured)} Caller => {Caller} - message => {Message}";
    }
  }
}
