using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Infrastructure;
using Anabasis.Common.Mediator;
using System;
using System.Threading.Tasks;

namespace Anabasis.Importer
{
  public abstract class BaseDocumentRepository<TConfiguration> : BaseActor, IDocumentRepository<TConfiguration> where TConfiguration : IAnabasisConfiguration
  {
    protected BaseDocumentRepository(TConfiguration configuration, SimpleMediator simpleMediator) : base(simpleMediator)
    {
      Configuration = configuration;
    }

    public TConfiguration Configuration { get; }

    public virtual Task OnExportEnd(ExportEnded endExport)
    {
      return Task.CompletedTask;
    }

    public virtual Task OnExportStarted(ExportStarted exportStarted)
    {
      return Task.CompletedTask;
    }

    public abstract Task SaveDocument(DocumentCreated documentExported);

    public abstract Task SaveIndex(IndexCreated indexExported);

    protected async override Task Handle(IEvent @event)
    {
      if (@event.GetType() == typeof(DocumentCreated))
      {
        var documentExported = @event as DocumentCreated;

        await SaveDocument(documentExported);

      }

      if (@event.GetType() == typeof(IndexCreated))
      {
        var indexExported = @event as IndexCreated;

        await SaveIndex(indexExported);

      }

      if (@event.GetType() == typeof(ExportStarted))
      {
        var exportStarted = @event as ExportStarted;

        await OnExportStarted(exportStarted);
      }

      if (@event.GetType() == typeof(ExportEnded))
      {
        var endExport = @event as ExportEnded;

        await OnExportEnd(endExport);
      }

    }

  }
}
