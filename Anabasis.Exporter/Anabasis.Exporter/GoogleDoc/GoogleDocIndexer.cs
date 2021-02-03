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
  public class GoogleDocIndexer : BaseActor
  {

    public GoogleDocIndexer(SimpleMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => StreamIds.GoogleDoc;

    public Task IndexDocument(DocumentCreated documentExported)
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

      Mediator.Emit(new IndexCreated(documentIndex, documentExported.CorrelationID, documentExported.StreamId, documentExported.TopicId));

      return Task.CompletedTask;
    }

    protected async override Task Handle(IEvent @event)
    {
      if (@event.GetType() == typeof(DocumentCreated))
      {
        await IndexDocument(@event as DocumentCreated);
      }
    }
  }
}
