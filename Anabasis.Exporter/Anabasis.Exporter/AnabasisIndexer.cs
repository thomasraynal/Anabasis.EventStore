using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Infrastructure;
using Anabasis.Common.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Exporter
{
  public class AnabasisIndexer : BaseActor
  {
    public AnabasisIndexer(SimpleMediator simpleMediator) : base(simpleMediator)
    {
    }

    public Task IndexDocument(DocumentExported documentExported)
    {

      var anabasisDocument = documentExported.Document;

      var documentIndex = new DocumentIndex()
      {
        Id = anabasisDocument.Id,
        Title = anabasisDocument.Title,
      };

      documentIndex.DocumentIndices = anabasisDocument.DocumentItems
        .Where(documentItem => documentItem.IsMainTitle)
        .Select(documentItem =>
        {

          var documentIndex = new DocumentIndex()
          {
            Id = documentItem.Id,
            Title = documentItem.Content,
          };

          documentIndex.DocumentIndices = anabasisDocument.DocumentItems
            .Where(documentSubItem => documentSubItem.MainTitleId == documentItem.Id && documentSubItem.IsSecondaryTitle)
            .Select(documentSubItem =>
            {
              return new DocumentIndex()
              {
                Id = documentSubItem.Id,
                Title = documentSubItem.Content,
              };

            }).ToArray();

          return documentIndex;
        }
        ).ToArray();

      Mediator.Emit(new IndexExported(documentIndex, documentExported.CorrelationID));

      return Task.CompletedTask;
    }

    protected async override Task Handle(IEvent @event)
    {
      if (@event.GetType() == typeof(DocumentExported))
      {
        await IndexDocument(@event as DocumentExported);
      }
    }
  }
}
