using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using Newtonsoft.Json;
using System;
using System.IO;
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

      AnabasisDocument anabasisDocument = null;

      if (documentExported.DocumentUri.IsFile)
      {
        anabasisDocument = JsonConvert.DeserializeObject<AnabasisDocument>(File.ReadAllText(documentExported.DocumentUri.AbsolutePath));
      }

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

 
      var indexPath = Path.GetFullPath($"{anabasisDocument.Id}-index");

      File.WriteAllText(indexPath, JsonConvert.SerializeObject(documentIndex));

      Mediator.Emit(new IndexCreated(documentIndex.Id, new Uri(indexPath), documentExported.CorrelationID, documentExported.StreamId, documentExported.TopicId));

      return Task.CompletedTask;
    }

  }
}
