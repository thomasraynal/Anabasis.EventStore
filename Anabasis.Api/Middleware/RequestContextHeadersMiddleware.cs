using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Anabasis.Api.Middleware
{
    public class RequestContextHeadersMiddleware
    {
    
        private readonly RequestDelegate _next;
        private readonly string _applicationName;

        public RequestContextHeadersMiddleware(RequestDelegate next, AppContext appContext)
        {
            _next = next;
            _applicationName = appContext.ApplicationName;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;

            var requestId = (
                request.Headers.TryGetValue(HttpHeaderConstants.HTTP_HEADER_REQUEST_ID, out var requestIdValue)
                && requestIdValue.Count != 0
                && Guid.TryParse(requestIdValue[0], out var requestIdFromHeader)
                )
                ? requestIdFromHeader
                : GuiDate.GenerateTimeBasedGuid();
            
            var correlationId = (
                request.Headers.TryGetValue(HttpHeaderConstants.HTTP_HEADER_CORRELATION_ID, out var correlationIdValue)
                && correlationIdValue.Count != 0
                && Guid.TryParse(correlationIdValue[0], out var correlationIdFromHeader)
                )
                ? correlationIdFromHeader
                : requestId;

            request.HttpContext.Items[HttpHeaderConstants.HTTP_HEADER_REQUEST_ID] = requestId;
            request.HttpContext.Items[HttpHeaderConstants.HTTP_HEADER_CORRELATION_ID] = correlationId;

            var response = context.Response;
            response.Headers[HttpHeaderConstants.HTTP_HEADER_APP_NAME] = _applicationName;
            response.Headers[HttpHeaderConstants.HTTP_HEADER_REQUEST_ID] = $"{requestId}";

            await _next(context);
        }
    }


}
