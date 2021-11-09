using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api.Middleware
{
    public class ApiVersionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _apiVersionNumber;

        public ApiVersionMiddleware(RequestDelegate next, int versionNumber)
        {
            _next = next;
            _apiVersionNumber = $"v{versionNumber}";
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers[HttpHeaderConstants.HTTP_HEADER_API_VERSION] = _apiVersionNumber;
            await _next(context);
        }
    }
}
