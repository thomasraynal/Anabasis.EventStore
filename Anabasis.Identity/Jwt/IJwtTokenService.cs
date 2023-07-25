using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Claims;

namespace Anabasis.Identity
{
    public interface IJwtTokenService<TUser>
    {
        (string token, DateTime expirationUtcDate) CreateToken(TUser user, params Claim[] additionalClaims);
        (bool isUserValid, SecurityToken securityToken) GetSecurityTokenFromString(string token);
    }
}
