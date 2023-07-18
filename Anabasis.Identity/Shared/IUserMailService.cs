using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity
{
    public interface IUserMailService
    {
        Task SendEmailPasswordReset(string email, string token);
        Task SendEmailConfirmationToken(string email, string token);
    }
}
