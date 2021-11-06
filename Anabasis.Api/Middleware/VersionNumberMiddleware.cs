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

        public VersionNumberMiddleware(RequestDelegate next, string versionNumber)
        {
            _next = next;
            _versionNumber = versionNumber;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers[WebConstants.API_VERSION] = _versionNumber;
            await _next(context);
        }
    }

    public static class VersionNumberExtension
    {
        public static IApplicationBuilder WithVersionNumber(this IApplicationBuilder app, string versionNumber)
        {
            app.UseMiddleware<VersionNumberMiddleware>(versionNumber);
            return app;
        }
    }
}
