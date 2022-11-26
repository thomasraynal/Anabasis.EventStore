using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity.Shared
{
    public interface IRegistrationDto
    {
        string Username { get;  }
        string Password { get; }
        string UserEmail { get; }
    }
}
