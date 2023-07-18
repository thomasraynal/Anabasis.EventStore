using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity.Shared
{
    public static class IdentityResultExtension
    {
        public static string FlattenErrors(this IdentityResult? identityResult)
        {
            if (null == identityResult)
            {
                return string.Empty;
            }

            return string.Join(", ", identityResult.Errors.Select(error => $"{error.Code} - {error.Description}").Distinct());
        }
    }
}
