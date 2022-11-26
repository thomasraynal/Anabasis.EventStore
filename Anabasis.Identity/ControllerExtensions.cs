using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity
{
    public static class ControllerExtensions
    {
        public static Task<TUser> GetCurrentUser<TUser>(this Controller controller, UserManager<TUser> userManager)
            where TUser : class
        {
            return userManager.GetUserAsync(controller.HttpContext.User);
        }
    }
}
