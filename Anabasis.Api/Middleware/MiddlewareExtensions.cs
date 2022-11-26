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

        //public static IApplicationBuilder WithHttpErrorFormatting(this IApplicationBuilder app)
        //{
        //    app.UseMiddleware<HttpErrorFormattingMiddleware>();
        //    return app;
        //}


        public static IApplicationBuilder WithRequestResponseLogging(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            return app;
        }

        public static IApplicationBuilder WithClientIPAddress(this IApplicationBuilder app)
        {
            app.UseMiddleware<IPAddressMiddleware>();
            return app;
        }
    }
}
