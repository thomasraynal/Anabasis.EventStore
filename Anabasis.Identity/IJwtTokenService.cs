using Microsoft.IdentityModel.Tokens;
using System;

namespace Anabasis.Identity
{
    public interface IJwtTokenService<TUser>
    {
        (string token, DateTime expirationUtcDate) CreateToken(TUser user);
        (bool isUserValid, SecurityToken securityToken) GetSecurityTokenFromString(string token);
    }
}
