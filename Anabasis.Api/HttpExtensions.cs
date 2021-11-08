using Microsoft.AspNetCore.Http;
using System;

namespace Anabasis.Api
{
    public static class HttpExtensions
    {
        public static Guid GetCorrelationId(this HttpRequest request) => request.HttpContext.GetCorrelationId();
        
        public static Guid GetCorrelationId(this HttpContext context)
        {
            if (!context.Items.TryGetValue(HttpHeaderConstants.HTTP_HEADER_CORRELATION_ID, out var correlationId))
            {
                return Guid.Empty;
            }
            return (Guid)correlationId;
        }
       
        public static void SetCorrelationId(this HttpRequest request, Guid guid) => request.HttpContext.SetCorrelationId(guid);
      
        public static void SetCorrelationId(this HttpContext context, Guid guid)
        {
            context.Items[HttpHeaderConstants.HTTP_HEADER_CORRELATION_ID] = guid;
        }

        public static Guid GetRequestId(this HttpRequest request) => request.HttpContext.GetRequestId();
        public static Guid GetRequestId(this HttpContext context)
        {
            if (!context.Items.TryGetValue(HttpHeaderConstants.HTTP_HEADER_REQUEST_ID, out var requestId))
            {
                return Guid.Empty;
            }
            return (Guid)requestId;
        }


    }
}
