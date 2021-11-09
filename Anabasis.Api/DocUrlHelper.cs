using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public static class DocUrlHelper
    {
        public static Uri GetDocUrl(string actionName)
        {
            return new Uri($"https://api-docs.beezup.com/#operation/{actionName}");
        }
    }
}
