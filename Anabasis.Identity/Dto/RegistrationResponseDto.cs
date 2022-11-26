using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity.Dto
{
    public class RegistrationResponseDto
    {
        public RegistrationResponseDto(string username, string email, string emailConfirmationToken)
        {
            Username = username;
            Email = email;
            EmailConfirmationToken = emailConfirmationToken;
        }

        public string Username { get; set; }
        public string Email { get; set; }
        public string EmailConfirmationToken { get; set; }
    }
}
