using Anabasis.Actor;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Exporter
{
  public class GoogleDocIndexer : BaseActor
  {
    public GoogleDocIndexer(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
    }

    public async Task Handle(DocumentCreated documentExported)
    {

      GoogleDocAnabasisDocument anabasisDocument = null;

      if (documentExported.DocumentUri.IsFile)
      {
        anabasisDocument = JsonConvert.DeserializeObject<GoogleDocAnabasisDocument>(File.ReadAllText(documentExported.DocumentUri.AbsolutePath));
      }
      else
      {
        throw new InvalidOperationException("not handled");
      }

      var documentIndex = new GoogleDocAnabasisDocument()
      {
        Id = anabasisDocument.Id,
        Title = anabasisDocument.Title,
      };

      documentIndex.Children = anabasisDocument.Children
        .Where(documentItem => documentItem.IsMainTitle)
        .Select(documentItem =>
        {

          var documentIndex = new GoogleDocAnabasisDocument()
          {
            Id = documentItem.Id,
            Title = documentItem.Content,
          };

          documentIndex.Children = anabasisDocument.Children
            .Where(documentSubItem => documentSubItem.MainTitleId == documentItem.Id && documentSubItem.IsSecondaryTitle)
            .Select(documentSubItem =>
            {
              return new GoogleDocAnabasisDocument()
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

      await Emit(new IndexCreated(documentIndex.Id, new Uri(indexPath), documentExported.CorrelationID, documentExported.StreamId));

    }

  }
}
