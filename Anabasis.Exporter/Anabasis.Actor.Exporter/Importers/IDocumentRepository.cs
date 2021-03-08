using Anabasis.Common;
using Anabasis.Common.Events;
using System.Threading.Tasks;

namespace Anabasis.Importer
{
  public interface IDocumentRepository<TConfiguration> where TConfiguration : IAnabasisConfiguration
  {
    Task Handle(DocumentCreated documentExported);
    Task Handle(IndexCreated indexExported);
    Task Handle(ExportStarted exportStarted);
    Task Handle(StartExportCommand startExportRequest);
  }
}
