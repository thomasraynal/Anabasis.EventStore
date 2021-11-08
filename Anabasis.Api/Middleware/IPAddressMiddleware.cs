using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Anabasis.Api.Middleware
{
    public class IPAddressMiddleware
    {
        readonly RequestDelegate _next;

        public IPAddressMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Items[HttpHeaderConstants.HTTP_HEADER_CLIENT_IP_ADRESSS] = context.Request.HttpContext.Connection.RemoteIpAddress;

             await _next(context);
        }
    }
}
