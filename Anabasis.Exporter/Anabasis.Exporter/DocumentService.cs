using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Exporter
{
  public class DocumentService : IDocumentService
  {
    private readonly ExporterConfiguration _exporterConfiguration;

    public DocumentService(ExporterConfiguration exporterConfiguration)
    {
      _exporterConfiguration = exporterConfiguration;
    }

    private async Task<string> GetAccessToken()
    {
      var tokenUrl = "https://oauth2.googleapis.com/token";

      var httpClient = new HttpClient();

      var formUrlEncodedContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _exporterConfiguration.ClientId),
            new KeyValuePair<string, string>("client_secret", _exporterConfiguration.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", _exporterConfiguration.RefreshToken)
        });


      var httpResponseMessage = await httpClient.PostAsync(tokenUrl, formUrlEncodedContent);

      var content = await httpResponseMessage.Content.ReadAsStringAsync();

      if (!httpResponseMessage.IsSuccessStatusCode) throw new InvalidOperationException($"{httpResponseMessage.StatusCode} - {content}");

      return JObject.Parse(content).Value<string>("access_token");

    }

    private async Task<TResponse> Get<TResponse>(string requestUrl)
    {

      var httpClient = new HttpClient();

      var accessToken = await GetAccessToken();

      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

      var httpResponseMessage = await httpClient.GetAsync(requestUrl);

      var content = await httpResponseMessage.Content.ReadAsStringAsync();

      if (!httpResponseMessage.IsSuccessStatusCode) throw new InvalidOperationException($"{httpResponseMessage.StatusCode} - {content}");

      return JsonConvert.DeserializeObject<TResponse>(content);

    }

    public async IAsyncEnumerable<AnabasisDocument> GetDocumentFromSource(string folderId)
    {
      var nextUrl = $"https://www.googleapis.com/drive/v2/files/{folderId}/children";

      while (!string.IsNullOrEmpty(nextUrl))
      {
        var childList = await Get<ChildList>(nextUrl);

        foreach (var child in childList.ChildReferences)
        {
          yield return await GetAnabasisDocument(child);

        }

        nextUrl = childList.NextLink;

      }

    }

    private async Task<AnabasisDocument> GetAnabasisDocument(ChildReference childReference)
    {

      var documentLite = await Get<DocumentLite>($"https://docs.googleapis.com/v1/documents/{childReference.Id}");

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

    public async Task ExportFolder()
    {
      var jsonSerializerSettings = new JsonSerializerSettings
      {

        ContractResolver = new DefaultContractResolver
        {
          NamingStrategy = new CamelCaseNamingStrategy()
        },

        Formatting = Formatting.Indented

      };

      var anabasisDocuments = new List<AnabasisDocument>();

      await foreach (var anabasisDocument in GetDocumentFromSource(_exporterConfiguration.DriveRootFolder))
      {
        anabasisDocuments.Add(anabasisDocument);
      }

      var exportJson = JsonConvert.SerializeObject(anabasisDocuments, Formatting.None, jsonSerializerSettings);

      File.WriteAllText(Path.Combine(_exporterConfiguration.LocalDocumentFolder, "export.json"), exportJson);

      var documentIndices = new List<DocumentIndex>();

      foreach (var anabasisDocument in anabasisDocuments)
      {
        var documentIndex = new DocumentIndex()
        {
          Id = anabasisDocument.Id,
          Title = anabasisDocument.Title,
        };

        documentIndex.DocumentIndices = anabasisDocument.DocumentItems
          .Where(documentItem => documentItem.IsMainTitle)
          .Select(documentItem=>
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


        documentIndices.Add(documentIndex);

      }

      var indexJson = JsonConvert.SerializeObject(documentIndices, Formatting.None, jsonSerializerSettings);

      File.WriteAllText(Path.Combine(_exporterConfiguration.LocalDocumentFolder, "index.json"), indexJson);

    }


    public Task<DocumentItem[]> GetDocumentItemsByMainSecondaryTitleId(string secondaryTitleId)
    {
      throw new NotImplementedException();
    }

    public Task<DocumentItem[]> GetDocumentItemsByMainTitleId(string mainTitleId)
    {
      throw new NotImplementedException();
    }

    public Task<Document[]> GetDocuments(bool fetchDocumentItems = false)
    {
      throw new NotImplementedException();
    }

    public Task<DocumentItem[]> GetMainTitlesByDocumentId(string documentId)
    {
      throw new NotImplementedException();
    }

    public Task<DocumentItem[]> GetSecondaryTitlesByMainTitleId(string mainTitle)
    {
      throw new NotImplementedException();
    }
  }
}
