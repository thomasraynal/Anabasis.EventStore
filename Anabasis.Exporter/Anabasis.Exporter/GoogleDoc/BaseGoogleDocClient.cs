using Anabasis.Common;
using Anabasis.Common.Mediator;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;


namespace Anabasis.Exporter.GoogleDoc
{
  public class GoogleDocClient
  {
    private readonly IAnabasisConfiguration _exporterConfiguration;
    private readonly PolicyBuilder _policyBuilder;

    public GoogleDocClient(IAnabasisConfiguration exporterConfiguration)
    {
      _exporterConfiguration = exporterConfiguration;

      _policyBuilder = Policy.Handle<Exception>();
    }

    public async Task<string> GetAccessToken()
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

    public async Task<TResponse> Get<TResponse>(string requestUrl)
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
  }
}
