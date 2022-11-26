using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity.Dto
{
    public class BearerTokenUserLoginResponse
    {
        public string? BearerToken { get; init; }
        public DateTime ExpirationUtcDate { get; init; }
    }
}
