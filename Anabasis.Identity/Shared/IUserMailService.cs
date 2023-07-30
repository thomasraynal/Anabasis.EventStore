using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity
{
    public interface IUserMailService
    {
        Task SendEmailPasswordResetAsync(string email, string token);
        Task SendEmailConfirmationTokenAsync(string email, string token);
    }
}
