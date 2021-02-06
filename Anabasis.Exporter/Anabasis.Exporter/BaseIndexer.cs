using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Exporter
{
  public abstract class BaseIndexer : BaseActor
  {

    public BaseIndexer(IMediator simpleMediator) : base(simpleMediator)
    {
    }

    public Task Handle(DocumentCreated documentExported)
    {

      var anabasisDocument = documentExported.Document;

      var documentIndex = new AnabasisDocumentIndex()
      {
        Id = anabasisDocument.Id,
        Title = anabasisDocument.Title,
      };

      documentIndex.DocumentIndices = anabasisDocument.DocumentItems
        .Where(documentItem => documentItem.IsMainTitle)
        .Select(documentItem =>
        {

          var documentIndex = new AnabasisDocumentIndex()
          {
            Id = documentItem.Id,
            Title = documentItem.Content,
          };

          documentIndex.DocumentIndices = anabasisDocument.DocumentItems
            .Where(documentSubItem => documentSubItem.MainTitleId == documentItem.Id && documentSubItem.IsSecondaryTitle)
            .Select(documentSubItem =>
            {
              return new AnabasisDocumentIndex()
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

  }
}
