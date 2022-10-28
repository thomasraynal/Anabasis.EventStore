using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Jwt
{
    public static class SimpleJwtAppBuilderExtensions
    {
        public static IApplicationBuilder WithSimpleJwtMiddleware<TUser>(this IApplicationBuilder applicationBuilder) where TUser : ISimpleJwtUser
        {
            return applicationBuilder.UseMiddleware<SimpleJwtMiddleware<TUser>>();
        }
    }
}
