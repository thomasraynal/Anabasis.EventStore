using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity
{
    public interface IPasswordResetMailService
    {
        Task SendEmailPasswordReset(string email, string token);
    }
}
