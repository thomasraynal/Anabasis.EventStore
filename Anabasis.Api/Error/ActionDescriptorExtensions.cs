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
            var controllerActionDescriptor = actionDescriptor as ControllerActionDescriptor;
            return controllerActionDescriptor?.ActionName ?? actionDescriptor?.DisplayName ?? "Unknown";
        }
    }
}
