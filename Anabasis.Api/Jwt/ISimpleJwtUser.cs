using System;
using System.Security.Claims;

namespace Anabasis.Api.Jwt
{
    public interface ISimpleJwtUser
    {
        Guid Id { get; }
        string[] GetUserRoles();
        Claim[] GetUserClaims();
    }
}
