using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using System;
using System.Threading.Tasks;

namespace Anabasis.Importer
{
  public interface IDocumentRepository<TConfiguration> : IActor where TConfiguration : IAnabasisConfiguration
  {
    Task SaveDocument(Guid exportId, AnabasisDocument anabasisDocument);
    Task SaveIndex(Guid exportId, DocumentIndex index);

    Task OnExportStarted(ExportStarted exportStarted);

    Task OnExportEnd(Guid exportId);
  }
}
