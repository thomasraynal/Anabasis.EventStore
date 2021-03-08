using Anabasis.Actor;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using Anabasis.Exporter.GoogleDoc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Exporter
{

  public class GoogleDocExporter : BaseActor
  {
    
    private GoogleDocClient _googleDocClient;

    public GoogleDocExporter(IAnabasisConfiguration exporterConfiguration, IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
      _googleDocClient = new GoogleDocClient(exporterConfiguration);
    }

    private async Task<AnabasisDocument> GetAnabasisDocument(string childReferenceId)
    {

      var documentLite = await _googleDocClient.Get<DocumentLite>($"https://docs.googleapis.com/v1/documents/{childReferenceId}");

      var rootDocumentid = documentLite.Title.GetReadableId();

      var documentItems = documentLite.Paragraphs.Select(paragraph => paragraph.ToDocumentItem(rootDocumentid))
                                           .Where(documentItem => !string.IsNullOrEmpty(documentItem.Content))
                                           .ToArray();

      string currentMainTitleId = null;
      string currentSecondaryTitleId = null;
      string parentId = null;

      var position = 0;

      foreach (var documentItem in documentItems)
      {

        documentItem.Position = position++;

        if (documentItem.IsMainTitle)
        {
          currentMainTitleId = documentItem.Id;
          currentSecondaryTitleId = null;
        }
        else
        {
          documentItem.MainTitleId = currentMainTitleId;
          documentItem.SecondaryTitleId = currentSecondaryTitleId;
        }

        if (documentItem.IsSecondaryTitle)
        {
          currentSecondaryTitleId = documentItem.Id;
        }

        documentItem.ParentId = parentId;

        parentId = documentItem.Id;

      }

      return new AnabasisDocument()
      {
        Id = rootDocumentid,
        Title = documentLite.Title,
        DocumentItems = documentItems
      };

    }

    public async Task Handle(ExportDocument exportDocument)
    {

      var anabasisDocument = await GetAnabasisDocument(exportDocument.DocumentId);

      var path = Path.GetFullPath($"{anabasisDocument.Id}");

      File.WriteAllText(path, JsonConvert.SerializeObject(anabasisDocument));

      Emit(new DocumentCreated(exportDocument.CorrelationID, exportDocument.StreamId, exportDocument.TopicId, anabasisDocument.Id, new Uri(path)));

    }
  }
}
