using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Jwt
{
    public static class HttpContextExtensions
    {
        public static TUser GetUser<TUser>(this HttpContext httpContext) where TUser : ISimpleJwtUser
        {
            return (TUser)httpContext.Items["User"];
        }
    }
}
