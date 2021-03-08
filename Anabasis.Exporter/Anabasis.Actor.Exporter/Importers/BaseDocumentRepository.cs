using Anabasis.Actor;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using System.Threading.Tasks;

namespace Anabasis.Importer
{
  public abstract class BaseDocumentRepository<TConfiguration> : BaseActor, IDocumentRepository<TConfiguration> where TConfiguration : IAnabasisConfiguration
  {
    protected BaseDocumentRepository(TConfiguration configuration, IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
      Configuration = configuration;
    }

    public TConfiguration Configuration { get; }

    public virtual Task Handle(StartExportCommand startExportRequest)
    {
      return Task.CompletedTask;
    }

    public virtual Task Handle(ExportStarted exportStarted)
    {
      return Task.CompletedTask;
    }

    public abstract Task Handle(DocumentCreated documentExported);

    public abstract Task Handle(IndexCreated indexExported);

  }
}
