using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Jwt
{
    public interface ISimpleJwtUser
    {
        Guid? UserId { get; }
        string UserMail { get; }
        string UserRole { get; }
    }
}
