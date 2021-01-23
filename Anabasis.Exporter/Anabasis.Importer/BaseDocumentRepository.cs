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

    public virtual Task OnExportEnded(Guid exportId)
    {
      return Task.CompletedTask;
    }

    public virtual Task OnExportStarted(Guid exportId)
    {
      return Task.CompletedTask;
    }

    public abstract Task SaveDocument(Guid exportId, AnabasisDocument anabasisDocument);

    public abstract Task SaveIndex(Guid exportId, DocumentIndex documentIndex);

    protected async override Task Handle(IEvent @event)
    {
      if (@event.GetType() == typeof(DocumentExported))
      {
        var documentExported = @event as DocumentExported;

        await SaveDocument(@event.CorrelationID, documentExported.Document);

      }

      if (@event.GetType() == typeof(IndexExported))
      {
        var indexExported = @event as IndexExported;

        await SaveIndex(@event.CorrelationID, indexExported.Index);

      }

      if (@event.GetType() == typeof(StartExport))
      {
        await OnExportStarted(@event.CorrelationID);
      }

      if (@event.GetType() == typeof(EndExport))
      {
        await OnExportEnded(@event.CorrelationID);
      }

    }

  }
}
