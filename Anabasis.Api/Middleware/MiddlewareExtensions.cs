using Microsoft.AspNetCore.Builder;

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

        public static IApplicationBuilder WithVersionNumber(this IApplicationBuilder app, int versionNumber)
        {
            app.UseMiddleware<VersionNumberMiddleware>(versionNumber);
            return app;
        }

    }
}
