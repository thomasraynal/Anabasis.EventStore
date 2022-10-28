using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Jwt
{
    public interface ISimpleJwtUserService<TUser> where TUser: ISimpleJwtUser
    {
        Task<TUser> GetUserById(Guid guid);
        Task<TUser> GetUserByEmail(string userEmail);
    }
}
