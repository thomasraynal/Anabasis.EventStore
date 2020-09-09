using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace Anabasis.Exporter
{
  class Program
  {
    static void Main(string[] args)
    {

      var childList = JsonConvert.DeserializeObject<ChildList>(File.ReadAllText("documents/childList.json"));


      var documentLite = JsonConvert.DeserializeObject<DocumentLite>(File.ReadAllText("documents/document.json"));

      var position = 0;

      var documentItems = documentLite.Paragraphs.Select(paragraph => paragraph.ToDocumentItem(documentLite))
                                                 .Where(documentItem => !string.IsNullOrEmpty(documentItem.Content))
                                                 .ToArray();

      string currentMainTitleId = null;
      string currentSecondaryTitleId = null;


      foreach (var documentItem in documentItems)
      {
        documentItem.MainTitleId = currentMainTitleId;
        documentItem.SecondaryTitleId = currentSecondaryTitleId;

        documentItem.Position = position++;

        if (documentItem.IsMainTitle)
        {
          currentMainTitleId = documentItem.Id;
          currentSecondaryTitleId = null;
        }

        if (documentItem.IsSecondaryTitle)
        {
          currentSecondaryTitleId = documentItem.Id;
        }

      }

      string parentId = null;

      foreach (var documentItem in documentItems)
      {

        documentItem.ParentId = parentId;

        parentId = documentItem.Id;

      }

      var title = "Qu’est ce que la philosophie antique?";

      var documentItemForTitle = documentItems.Where(documentItem => documentItem.IsMainTitle && documentItem.Content == title).Last();

      var documentItemsForTitle = documentItems.Where(documentItem => documentItem.MainTitleId == documentItemForTitle.Id);


      //create subdocument
    }
  }
}
