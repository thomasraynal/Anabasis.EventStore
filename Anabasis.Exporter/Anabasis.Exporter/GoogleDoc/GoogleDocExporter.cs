using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Infrastructure;
using Anabasis.Common.Mediator;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Anabasis.Exporter
{
  public class GoogleDocExporter: BaseActor
  {
    private readonly IAnabasisConfiguration _exporterConfiguration;
    private readonly PolicyBuilder _policyBuilder;

    public override string StreamId => StreamIds.GoogleDoc;

    public GoogleDocExporter(IAnabasisConfiguration exporterConfiguration, SimpleMediator simpleMediator): base(simpleMediator)
    {
      _exporterConfiguration = exporterConfiguration;

      _policyBuilder = Policy.Handle<Exception>();
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
      var retryPolicy = _policyBuilder.WaitAndRetry(5, (_) => TimeSpan.FromSeconds(1));

      return await retryPolicy.Execute(async () =>
      {
        var httpClient = new HttpClient();

        var accessToken = await GetAccessToken();

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var httpResponseMessage = await httpClient.GetAsync(requestUrl);

        var content = await httpResponseMessage.Content.ReadAsStringAsync();

        if (!httpResponseMessage.IsSuccessStatusCode) throw new InvalidOperationException($"{httpResponseMessage.StatusCode} - {content}");

        return JsonConvert.DeserializeObject<TResponse>(content);

      });

    }

    private async IAsyncEnumerable<AnabasisDocument> GetDocumentFromSource(StartExport startExport, string folderId)
    {
      var nextUrl = $"https://www.googleapis.com/drive/v2/files/{folderId}/children";

      while (!string.IsNullOrEmpty(nextUrl))
      {
        var childList = await Get<ChildList>(nextUrl);

        //only have one folder - but we should handle many and keep track of the original gdoc id
        Mediator.Emit(new ExportStarted(startExport.CorrelationID,
          childList.ChildReferences.Select(reference=> reference.Id).ToArray(),
          startExport.StreamId,
          startExport.TopicId));

        foreach (var child in childList.ChildReferences)
        {
          yield return await GetAnabasisDocument(startExport, child);

        }

        nextUrl = childList.NextLink;

      }

    }

    private async Task<AnabasisDocument> GetAnabasisDocument(StartExport startExport, ChildReference childReference)
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

    public async Task ExportDocuments(StartExport startExport)
    {

      await foreach (var anabasisDocument in GetDocumentFromSource(startExport, _exporterConfiguration.DriveRootFolder))
      {

        Mediator.Emit(new DocumentCreated(startExport.CorrelationID, startExport.StreamId, startExport.TopicId, anabasisDocument));
      }

      Mediator.Emit(new ExportFinished(startExport.CorrelationID, startExport.StreamId, startExport.TopicId));

    }

    protected async override Task Handle(IEvent @event)
    {

      if (@event.GetType() == typeof(StartExport))
      {
        await ExportDocuments(@event as StartExport);
      }
       
    }
  }
}
