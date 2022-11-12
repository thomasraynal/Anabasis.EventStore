using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Contracts
{
    public interface IAnabasisUserStore<TUser> : IUserStore<TUser>,
                                 IUserClaimStore<TUser>,
                                 IUserLoginStore<TUser>,
                                 IUserRoleStore<TUser>,
                                 IUserPasswordStore<TUser>,
                                 IUserSecurityStampStore<TUser>
        where TUser : IdentityUser<Guid>
    {
    }
}
