using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using System;
using System.Threading.Tasks;

namespace Anabasis.Importer
{
  public interface IDocumentRepository<TConfiguration> : IActor where TConfiguration : IAnabasisConfiguration
  {
    Task Handle(DocumentCreated documentExported);
    Task Handle(IndexCreated indexExported);
    Task Handle(ExportStarted exportStarted);
    Task Handle(ExportEnded endExport);
  }
}
