using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class TitleDefined : BaseAnabasisExporterEvent
  {
    public TitleDefined(Guid correlationId, string streamId,  string documentId, string titleId) : base(correlationId, streamId)
    {
      DocumentId = documentId;
      Title = titleId;
    }

    public string DocumentId { get; set; }
    public string Title { get; set; }

    public override string Log()
    {
      return $"{nameof(TitleDefined)} {Title}";
    }
  }
}
