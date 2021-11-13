using Anabasis.Api.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Middleware
{
    ///https://elanderson.net/2019/12/log-requests-and-responses-in-asp-net-core-3/
    ///// waiting for => https://docs.microsoft.com/fr-fr/aspnet/core/fundamentals/http-logging/?view=aspnetcore-6.0
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger) 
        {
            _next = next;
            _logger = logger;
        }

        public Guid GetRequestIdFromRequest(HttpRequest request)
        {
            var errorMessage = $"{HttpHeaderConstants.HTTP_HEADER_REQUEST_ID} http request header is missing or not formatted correctly !";

            Guid requestId;

            if (request.Headers.TryGetValue(HttpHeaderConstants.HTTP_HEADER_REQUEST_ID, out var values))
            {
                var ids = values.ToArray();
                if (ids.Length != 1 || !Guid.TryParse(ids[0], out requestId))
                    throw new HttpRequestException(errorMessage);
            }
            else
            {
                requestId = GuiDate.GenerateTimeBasedGuid();
                request.Headers.Add(HttpHeaderConstants.HTTP_HEADER_REQUEST_ID, requestId.ToString());
            }

            return requestId;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            var requestId = GetRequestIdFromRequest(context.Request);
      

            await LogRequestAsync(requestId, context.Request);
            await LogResponseAsync(requestId, context);

            await _next(context);
        }

        private async Task LogRequestAsync(Guid requestId, HttpRequest request)
        {
            var fileContent = await GetRequestStringAsync(request);

            _logger.LogInformation(fileContent);

          //  request.HttpContext.Items[HttpHeaderConstants.HTTP_HEADER_REQUEST_LOGGING_URL] = requestFileName;

        }

        private async Task<string> GetRequestStringAsync(HttpRequest request)
        {
            request.EnableBuffering();

            var bodyAsText = string.Empty;

            using (var stream = request.Body)
            {
                using var reader = new StreamReader(stream);

                bodyAsText = await reader.ReadToEndAsync();
            }

            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyAsText));

            var path = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            var headers = string.Join(Environment.NewLine, request.Headers.Select(header => $"{header.Key}: {header.Value}"));

            return
                $"{request.Method} {path}" +
                    $"{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                $"Headers:" +
                  $"{Environment.NewLine}" +
                $"{headers}" +
                    $"{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                $"Body:" +
                    $"{Environment.NewLine}" +
                $"{bodyAsText}";
        }


        private async Task LogResponseAsync(
            Guid requestId, 
            HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();

            context.Response.Body = responseBody;

            await _next(context);

            var fileContent = await GetHttpResponseAsString(context.Response);

            _logger.LogInformation(fileContent);

            //   context.Request.HttpContext.Items[HttpHeaderConstants.HTTP_HEADER_RESPONSE_LOGGING_URL] = responseFile;

            await responseBody.CopyToAsync(originalBodyStream);

        }

        private async Task<string> GetHttpResponseAsString(HttpResponse response)
        {

            response.Body.Seek(0, SeekOrigin.Begin);

            var streamReader = new StreamReader(response.Body);

            var bodyAsText = await streamReader.ReadToEndAsync();

            response.Body.Seek(0, SeekOrigin.Begin);

            var headers = string.Join(Environment.NewLine, response.Headers.Select(header => $"{header.Key}: {header.Value}"));

            return
                $"ResponseStatus: {response.StatusCode}" +
                    $"{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                $"Headers:" +
                    $"{Environment.NewLine}" +
                $"{headers}" +
                    $"{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                $"Body:" +
                    $"{Environment.NewLine}" +
                $"{bodyAsText}";
        }
    }
}
