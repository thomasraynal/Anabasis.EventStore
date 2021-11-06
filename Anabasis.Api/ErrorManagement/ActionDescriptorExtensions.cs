using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public static class ActionDescriptorExtensions
    {
        public static string GetActionName(this ActionDescriptor actionDescriptor)
        {
            var tmp = actionDescriptor as ControllerActionDescriptor;
            return tmp?.ActionName ?? actionDescriptor?.DisplayName ?? "Unknown";
        }
    }
}
