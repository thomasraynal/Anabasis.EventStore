using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity.Shared
{
    public interface IRegistrationDto
    {
        string UserName { get;  }
        string Password { get; }
        string UserMail { get; }
    }
}
