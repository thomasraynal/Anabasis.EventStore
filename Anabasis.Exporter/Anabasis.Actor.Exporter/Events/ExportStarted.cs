using System;

namespace Anabasis.Common.Events
{
  public class ExportStarted : BaseAnabasisExporterEvent
  {
    public ExportStarted(Guid correlationId, string[] documentsIds, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
      DocumentsIds = documentsIds;
    }

    public string[] DocumentsIds { get; }

    public override string Log()
    {
      return $"{nameof(ExportStarted)} ({DocumentsIds.Length} document(s))";
    }
  }
}
