using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api.Middleware
{
    public class VersionNumberMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _versionNumber;

        public VersionNumberMiddleware(RequestDelegate next, int versionNumber)
        {
            _next = next;
            _versionNumber = $"v{versionNumber}";
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers[HttpHeaderConstants.HTTP_HEADER_API_VERSION] = _versionNumber;
            await _next(context);
        }
    }
}
