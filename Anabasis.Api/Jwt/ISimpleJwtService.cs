using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Jwt
{
    public interface ISimpleJwtService
    {
        (string token, DateTime expirationUtcDate) BuildToken(ISimpleJwtUser user);
        (bool isUserValid, SecurityToken? securityToken) GetToken(string token);
    }
}
