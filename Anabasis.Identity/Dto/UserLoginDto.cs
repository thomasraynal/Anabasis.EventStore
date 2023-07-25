using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity.Dto
{
    public class UserLoginDto : IUserLoginDto
    {
        [Required(ErrorMessage = "Username is required")]
        public string? Username { get; init; }

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; init; }
    }
}
