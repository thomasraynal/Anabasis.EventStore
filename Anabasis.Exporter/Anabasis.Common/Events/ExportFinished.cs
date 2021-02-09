using Anabasis.Common.Infrastructure;
using System;

namespace Anabasis.Common.Events
{
  public class ExportFinished : BaseEvent, ICommand
  {
    public ExportFinished(string callerId, Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
      CallerId = callerId;
    }

    public string CallerId { get; };

    public override string Log()
    {
      return "Export has finished";
    }
  }
}

