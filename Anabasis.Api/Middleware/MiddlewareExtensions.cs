﻿using Microsoft.AspNetCore.Builder;

namespace Anabasis.Api.Middleware
{
    public static class MiddlewareExtension
    {
        public static IApplicationBuilder WithRequestContextHeaders(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestContextHeadersMiddleware>();
            return app;
        }

        public static IApplicationBuilder WithClientIPAddress(this IApplicationBuilder app)
        {
            app.UseMiddleware<IPAddressMiddleware>();
            return app;
        }
    }
}
