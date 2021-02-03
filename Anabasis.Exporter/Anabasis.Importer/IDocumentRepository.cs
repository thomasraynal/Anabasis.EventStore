using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using System;
using System.Threading.Tasks;

namespace Anabasis.Importer
{
  public interface IDocumentRepository<TConfiguration> : IActor where TConfiguration : IAnabasisConfiguration
  {
    Task SaveDocument(DocumentCreated documentExported);
    Task SaveIndex(IndexCreated indexExported);

    Task OnExportStarted(ExportStarted exportStarted);

    Task OnExportEnd(ExportEnded endExport);
  }
}
