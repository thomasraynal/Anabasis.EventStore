using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Jwt
{
    public class SimpleJwtMiddleware<TUser> : IMiddleware where TUser : ISimpleJwtUser
    {
        public const string UserProperty = "User";

        private readonly ISimpleJwtUserService<TUser> _userService;
        private readonly ISimpleJwtService _tokenService;

        public SimpleJwtMiddleware(ISimpleJwtUserService<TUser> userService, ISimpleJwtService tokenService)
        {

            _userService = userService;
            _tokenService = tokenService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
                await AttachUserToContext(context, token);

            await next(context);
        }

        private async Task AttachUserToContext(HttpContext context, string token)
        {
            try
            {
                var (isUserValid, securityToken) = _tokenService.GetToken(token);

                if (isUserValid)
                {
                    var userId = securityToken.Id;

                    context.Items[UserProperty] = await _userService.GetUserById(Guid.Parse(userId));
                }
            }
            catch
            {
            }
        }
    }
}
